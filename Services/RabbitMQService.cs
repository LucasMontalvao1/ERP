using API.Models.Configurations;
using API.Models.DTOs.Webhook;
using API.Services.Interfaces;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;


namespace API.Services
{
    public class RabbitMQService : IRabbitMQService, IDisposable
    {
        private readonly RabbitMQConfig _config;
        private readonly ILogger<RabbitMQService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private IConnection? _connection;
        private IChannel? _channel;
        private bool _disposed = false;

        public RabbitMQService(
            IOptions<RabbitMQConfig> config,
            ILogger<RabbitMQService> logger)
        {
            _config = config.Value;
            _logger = logger;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            InitializeConnection();
        }

        private async void InitializeConnection()
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _config.HostName,
                    Port = _config.Port,
                    UserName = _config.UserName,
                    Password = _config.Password,
                    VirtualHost = _config.VirtualHost,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                };

                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                // Declarar filas
                await DeclareQueues();

                _logger.LogInformation("Conexão RabbitMQ estabelecida com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao conectar com RabbitMQ");
                throw;
            }
        }

        private async Task DeclareQueues()
        {
            if (_channel is null) return;

            // Declarar filas com durabilidade
            await _channel.QueueDeclareAsync(_config.Queues.Integration, durable: true, exclusive: false, autoDelete: false);
            await _channel.QueueDeclareAsync(_config.Queues.Email, durable: true, exclusive: false, autoDelete: false);
            await _channel.QueueDeclareAsync(_config.Queues.Webhook, durable: true, exclusive: false, autoDelete: false);

            // Declarar filas DLQ (Dead Letter Queue)
            await _channel.QueueDeclareAsync($"{_config.Queues.Integration}_dlq", durable: true, exclusive: false, autoDelete: false);
            await _channel.QueueDeclareAsync($"{_config.Queues.Email}_dlq", durable: true, exclusive: false, autoDelete: false);
            await _channel.QueueDeclareAsync($"{_config.Queues.Webhook}_dlq", durable: true, exclusive: false, autoDelete: false);
        }

        public async Task PublishIntegrationMessageAsync(object message, string correlationId)
        {
            try
            {
                var json = JsonSerializer.Serialize(message, _jsonOptions);
                var body = Encoding.UTF8.GetBytes(json);

                var properties = new BasicProperties
                {
                    Persistent = true,
                    CorrelationId = correlationId,
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                    MessageId = Guid.NewGuid().ToString()
                };

                await _channel!.BasicPublishAsync(
                    exchange: "",
                    routingKey: _config.Queues.Integration,
                    mandatory: false,
                    basicProperties: properties,
                    body: body);

                _logger.LogInformation("Mensagem de integração publicada. CorrelationId: {CorrelationId}", correlationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao publicar mensagem de integração. CorrelationId: {CorrelationId}", correlationId);
                throw;
            }
        }

        public async Task PublishEmailMessageAsync(object message, string correlationId)
        {
            try
            {
                var emailMessage = new EmailMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    To = ExtractEmailRecipient(message),
                    Subject = ExtractEmailSubject(message),
                    Body = JsonSerializer.Serialize(message, _jsonOptions),
                    IsHtml = true
                };

                var json = JsonSerializer.Serialize(emailMessage, _jsonOptions);
                var body = Encoding.UTF8.GetBytes(json);

                var properties = new BasicProperties
                {
                    Persistent = true,
                    CorrelationId = correlationId,
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                    MessageId = emailMessage.Id,
                    Priority = 5
                };

                await _channel!.BasicPublishAsync(
                    exchange: "",
                    routingKey: _config.Queues.Email,
                    mandatory: false,
                    basicProperties: properties,
                    body: body);

                _logger.LogInformation("Mensagem de email publicada. CorrelationId: {CorrelationId}", correlationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao publicar mensagem de email. CorrelationId: {CorrelationId}", correlationId);
                throw;
            }
        }

        public async Task PublishWebhookMessageAsync(object message, string correlationId)
        {
            try
            {
                var json = JsonSerializer.Serialize(message, _jsonOptions);
                var body = Encoding.UTF8.GetBytes(json);

                var properties = new BasicProperties
                {
                    Persistent = true,
                    CorrelationId = correlationId,
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                    MessageId = Guid.NewGuid().ToString()
                };

                await _channel!.BasicPublishAsync(
                    exchange: "",
                    routingKey: _config.Queues.Webhook,
                    mandatory: false,
                    basicProperties: properties,
                    body: body);

                _logger.LogInformation("Mensagem de webhook publicada. CorrelationId: {CorrelationId}", correlationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao publicar mensagem de webhook. CorrelationId: {CorrelationId}", correlationId);
                throw;
            }
        }

        public async Task<List<EmailMessage>> ConsumeEmailQueueAsync(int batchSize = 50)
        {
            var emailMessages = new List<EmailMessage>();

            try
            {
                if (_channel is null)
                {
                    _logger.LogWarning("Canal RabbitMQ não disponível para consumo");
                    return emailMessages;
                }

                for (int i = 0; i < batchSize; i++)
                {
                    var result = await _channel.BasicGetAsync(_config.Queues.Email, autoAck: false);
                    if (result is null) break;

                    try
                    {
                        var body = result.Body.ToArray();
                        var json = Encoding.UTF8.GetString(body);
                        var emailMessage = JsonSerializer.Deserialize<EmailMessage>(json, _jsonOptions);

                        if (emailMessage != null)
                        {
                            emailMessage.Id = result.BasicProperties.MessageId ?? Guid.NewGuid().ToString();
                            emailMessages.Add(emailMessage);
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao deserializar mensagem de email");
                        await _channel.BasicNackAsync(result.DeliveryTag, false, false); 
                    }
                }

                _logger.LogDebug("Consumidas {Count} mensagens de email da fila", emailMessages.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consumir fila de emails");
            }

            return emailMessages;
        }

        public async Task AckEmailMessageAsync(string messageId)
        {
            try
            {
                _logger.LogDebug("Email ACK confirmado para mensagem: {MessageId}", messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao confirmar ACK do email: {MessageId}", messageId);
            }
        }

        public async Task NackEmailMessageAsync(string messageId)
        {
            try
            {
                _logger.LogWarning("Email NACK enviado para mensagem: {MessageId}", messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar NACK do email: {MessageId}", messageId);
            }
        }

        public async Task<bool> IsConnectedAsync()
        {
            try
            {
                return _connection != null && _connection.IsOpen && _channel != null && _channel.IsOpen;
            }
            catch
            {
                return false;
            }
        }

        public async Task<object> GetQueueStatsAsync(string queueName)
        {
            try
            {
                if (_channel is null)
                {
                    return new { Error = "Canal não disponível" };
                }

                var queueInfo = await _channel.QueueDeclarePassiveAsync(queueName);

                return new
                {
                    QueueName = queueName,
                    MessageCount = queueInfo.MessageCount,
                    ConsumerCount = queueInfo.ConsumerCount,
                    IsConnected = await IsConnectedAsync(),
                    CheckedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter estatísticas da fila: {QueueName}", queueName);
                return new { Error = ex.Message, QueueName = queueName };
            }
        }

        // Métodos auxiliares privados
        private string ExtractEmailRecipient(object message)
        {
            if (message is string jsonString)
            {
                try
                {
                    var doc = JsonDocument.Parse(jsonString);
                    if (doc.RootElement.TryGetProperty("to", out var toElement))
                    {
                        return toElement.GetString() ?? "lucasdesouza015@gmail.com";
                    }
                }
                catch
                {
                }
            }

            return "lucasdesouza015@gmail.com"; 
        }

        private string ExtractEmailSubject(object message)
        {
            var messageType = message.GetType().Name;

            return messageType switch
            {
                "IntegrationSuccess" => "Integração realizada com sucesso",
                "IntegrationError" => "Erro na integração",
                "DataUpdated" => "Dados atualizados",
                _ => "Notificação do Sistema ERP"
            };
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    _channel?.CloseAsync();
                    _channel?.Dispose();
                    _connection?.CloseAsync();
                    _connection?.Dispose();

                    _logger.LogInformation("Conexão RabbitMQ fechada");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao fechar conexão RabbitMQ");
                }

                _disposed = true;
            }
        }
    }
}