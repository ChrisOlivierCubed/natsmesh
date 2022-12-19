namespace ApplicationSettings;

public class Settings
{
  public MessageQueueDetails MessageQueueDetails { get; set; }
  public ProducerEventHubDetails ProducerEventHubDetails {get; set;}
  public ConsumerEventHubDetails ConsumerEventHubDetails { get; set; }

}
public class MessageQueueDetails
{
  public bool Enabled { get; set; }
  public string Url { get; set; }
}
public class ProducerEventHubDetails
{
  public string ConnectionString { get; set; }
  public string EventHub { get; set; }
}
public class ConsumerEventHubDetails
{
  public string ConnectionString { get; set; }
  public string EventHub { get; set; }
  public string ConsumerGroup { get; set; }
}