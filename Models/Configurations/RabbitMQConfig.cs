namespace API.Models.Configurations;

public class RabbitMQConfig
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public QueueConfig Queues { get; set; } = new();
}

public class QueueConfig
{
    public string Integration { get; set; } = "integration";
    public string Email { get; set; } = "emails";
    public string Webhook { get; set; } = "webhooks";
}