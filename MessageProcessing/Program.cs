using Microsoft.Extensions.Configuration;
using NATS.Client;
using System.Text;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Identity;
using ApplicationSettings;

namespace MessageProcessing;
class Program
{
  static async Task Main(string[] args)
  {
    int numOfEvents = 10;

    IConfigurationRoot configuration = new ConfigurationBuilder()
      .SetBasePath(Directory.GetCurrentDirectory())
      .AddJsonFile("appsettings.json", false, true)
      .AddEnvironmentVariables()
      .Build();

    var applicationSettings = new Settings();
    configuration.GetSection("AppSettings").Bind(applicationSettings);

    // create a connection to the NATS server
    IConnection connection = new ConnectionFactory().CreateConnection();

    // create eh producer and consumer
    EventHubProducerClient eventHubProducerClient = 
      new EventHubProducerClient(applicationSettings.ProducerEventHubDetails.ConnectionString
        ,applicationSettings.ProducerEventHubDetails.EventHub);
    EventHubConsumerClient eventHubConsumerClient = new EventHubConsumerClient(applicationSettings.ConsumerEventHubDetails.ConsumerGroup
        ,applicationSettings.ConsumerEventHubDetails.ConnectionString
        ,applicationSettings.ConsumerEventHubDetails.EventHub);
    // set read event hub options
    ReadEventOptions options = new ReadEventOptions { MaximumWaitTime = TimeSpan.FromSeconds(5) };

    // Send messages to event hub
    await SendMessages(eventHubProducerClient, numOfEvents);

    // Read from event Hub and send to nats
    await foreach (PartitionEvent partitionEvent in eventHubConsumerClient.ReadEventsAsync(options))
    {
      connection.Publish("clientInteractions", partitionEvent.Data.EventBody.ToArray());
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

