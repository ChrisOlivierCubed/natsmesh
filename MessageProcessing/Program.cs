using Microsoft.Extensions.Configuration;
using NATS.Client;
using System.Text;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Azure.Messaging.EventHubs.Consumer;

namespace MessageProcessing;
class Program
{
  protected readonly IConfiguration _configuration;
  static async Task Main(string[] args)
  {
    int numOfEvents = 10;

    // create a connection to the NATS server
    IConnection connection = new ConnectionFactory().CreateConnection();
    
    // create eh producer and consumer
    EventHubProducerClient eventHubProducerClient = new EventHubProducerClient("");
    EventHubConsumerClient eventHubConsumerClient = new EventHubConsumerClient("natstest", "");
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

