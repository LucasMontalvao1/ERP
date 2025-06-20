using API.Models.DTOs.Webhook;

namespace API.Services.Interfaces;

public interface IRabbitMQService
{
    Task PublishIntegrationMessageAsync(object message, string correlationId);
    Task PublishEmailMessageAsync(object message, string correlationId);
    Task PublishWebhookMessageAsync(object message, string correlationId);
    Task<List<EmailMessage>> ConsumeEmailQueueAsync(int batchSize = 50);
    Task AckEmailMessageAsync(string messageId);
    Task NackEmailMessageAsync(string messageId);
    Task<bool> IsConnectedAsync();
    Task<object> GetQueueStatsAsync(string queueName);
}