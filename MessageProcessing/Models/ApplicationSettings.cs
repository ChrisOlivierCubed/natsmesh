namespace ApplicationSettings;

public class Settings
{
  public NatsQueueDetails NatsQueueDetails { get; set; }
  public ProducerDetails ProducerDetails {get; set;}
  public ConsumerDetails ConsumerDetails { get; set; }

}
public class NatsQueueDetails
{
  public bool Enabled { get; set; }
  public string Url { get; set; }
}
public class ProducerDetails
{
  public string ConnectionString { get; set; }
  public string EventHub { get; set; }
}
public class ConsumerDetails
{
  public string ConnectionString { get; set; }
  public string EventHubNamespace { get; set; }
  public string EventHub { get; set; }
  public string ConsumerGroup { get; set; }
  public string BlobStorageAccount { get; set; }
  public string BlobContainer { get; set; }
}