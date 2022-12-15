using Microsoft.Extensions.Configuration;
using NATS.Client;
using System;
using System.Threading;
using System.Text;

namespace MessageProcessing;
class Program
{
  protected readonly IConfiguration _configuration;
  static void Main(string[] args)
  {
    // create a connection to the NATS server
    IConnection connection = new ConnectionFactory().CreateConnection();

    // subscribe to a subject
    var sub = connection.SubscribeAsync("my_subject");
    sub.MessageHandler += (sender, args) =>
    {
      Console.WriteLine($"Received message: {args.Message.Data}");
    };

    // publish a message to the subject
    connection.Publish("my_subject", Encoding.UTF8.GetBytes("Hello NATS!"));

    // wait for a key press to exit the app
    Console.ReadKey();

    // clean up
    sub.Unsubscribe();
    connection.Close();
  }
}
