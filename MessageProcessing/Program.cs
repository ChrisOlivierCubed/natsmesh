using Microsoft.Extensions.Configuration;
using NATS.Client;
using System.Text;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using Azure.Identity;
using ApplicationSettings;
using Azure.Storage.Blobs;

namespace MessageProcessing;
class Program
{
  static async Task Main(string[] args)
  {
    int numOfEvents = 10;
    var someTestVar = 4;

    IConfigurationRoot configuration = new ConfigurationBuilder()
      .SetBasePath(Directory.GetCurrentDirectory())
      .AddJsonFile("appsettings.json", false, true)
      .AddEnvironmentVariables()
      .Build();

    var applicationSettings = new Settings();
    configuration.GetSection("AppSettings").Bind(applicationSettings);

    // create a connection to the NATS server
    IConnection connection = new ConnectionFactory().CreateConnection(applicationSettings.NatsQueueDetails.Url);

    // create eh producer and consumer
    EventHubProducerClient eventHubProducerClient =
      new EventHubProducerClient(applicationSettings.ProducerDetails.ConnectionString
        , applicationSettings.ProducerDetails.EventHub);
    EventHubConsumerClient eventHubConsumerClient = new EventHubConsumerClient(applicationSettings.ConsumerDetails.ConsumerGroup
        , applicationSettings.ConsumerDetails.ConnectionString
        , applicationSettings.ConsumerDetails.EventHub);
    // set read event hub options
    ReadEventOptions options = new ReadEventOptions { MaximumWaitTime = TimeSpan.FromSeconds(5) };

    // Send messages to event hub
    await SendMessages(eventHubProducerClient, numOfEvents);

    // Set storage account for consumer

    BlobContainerClient storageClient = new BlobContainerClient(
        new Uri($"https://{applicationSettings.ConsumerDetails.BlobStorageAccount}.blob.core.windows.net/{applicationSettings.ConsumerDetails.BlobContainer}"), new DefaultAzureCredential());

    var processor = new EventProcessorClient(
    storageClient,
    applicationSettings.ConsumerDetails.ConsumerGroup,
    applicationSettings.ConsumerDetails.EventHubNamespace,
    applicationSettings.ConsumerDetails.EventHub,
    new DefaultAzureCredential());

    // Register handlers for processing events and handling errors
    processor.ProcessEventAsync += ProcessEventHandler;
    processor.ProcessErrorAsync += ProcessErrorHandler;

    // Start the processing
    await processor.StartProcessingAsync();

    // Wait for 30 seconds for the events to be processed
    await Task.Delay(TimeSpan.FromSeconds(30));

    // Stop the processing
    await processor.StopProcessingAsync();

    async Task ProcessEventHandler(ProcessEventArgs eventArgs)
    {
      // Publish the event to NATS
      connection.Publish("clientInteractions", eventArgs.Data.EventBody.ToArray());

      // Update checkpoint in the blob storage so that the app receives only new events the next time it's run
      await eventArgs.UpdateCheckpointAsync(eventArgs.CancellationToken);
    }

    Task ProcessErrorHandler(ProcessErrorEventArgs eventArgs)
    {
      // Write details about the error to the console window
      Console.WriteLine($"\tPartition '{eventArgs.PartitionId}': an unhandled exception was encountered. This was not expected to happen.");
      Console.WriteLine(eventArgs.Exception.Message);
      return Task.CompletedTask;
    }

    connection.Close();
  }
  static async Task SendMessages(EventHubProducerClient producerClient, int numOfEvents)
  {
    // send batch of events
    using EventDataBatch eventDataBatch = await producerClient.CreateBatchAsync();

    for (int i = 1; i <= numOfEvents; i++)
    {

      if (!eventDataBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes($"{DateTime.Now.ToString()} - My new event {i}"))))
      {
        // if event is too large for max batch size
        throw new Exception($"Event {i} is too large for the batch and cannot be sent.");
      }
    }

    try
    {
      // send batch of events through the producer
      await producerClient.SendAsync(eventDataBatch);
      Console.WriteLine($"A batch of {numOfEvents} events has been published.");
    }

    finally
    {
      await producerClient.DisposeAsync();
    }
  }

}

