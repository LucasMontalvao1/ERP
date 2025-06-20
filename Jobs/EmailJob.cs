using API.Jobs.Base;
using API.Models.DTOs.Webhook;
using API.Services.Interfaces;
using Hangfire;

namespace API.Jobs
{

    /// <summary>
    /// Job para processamento de emails
    /// </summary>
    public class EmailJob : BaseJob
    {
        private readonly IEmailService _emailService;
        private readonly IRabbitMQService _rabbitMQService;

        public EmailJob(
            IEmailService emailService,
            IRabbitMQService rabbitMQService,
            ILogger<EmailJob> logger) : base(logger)
        {
            _emailService = emailService;
            _rabbitMQService = rabbitMQService;
        }

        [Queue("email")]
        public async Task ProcessEmailQueueAsync()
        {
            await ExecuteWithErrorHandling(async () =>
            {
                var emailsToProcess = await _rabbitMQService.ConsumeEmailQueueAsync(50);

                if (!emailsToProcess.Any())
                {
                    _logger.LogDebug("Nenhum email pendente para processamento");
                    return;
                }

                _logger.LogInformation("Processando {Count} emails da fila", emailsToProcess.Count);

                var processedCount = 0;
                var successCount = 0;

                foreach (var emailMessage in emailsToProcess)
                {
                    try
                    {
                        var result = await _emailService.SendEmailAsync(emailMessage);
                        processedCount++;

                        if (result.Success)
                        {
                            successCount++;
                            await _rabbitMQService.AckEmailMessageAsync(emailMessage.Id);
                        }
                        else
                        {
                            await _rabbitMQService.NackEmailMessageAsync(emailMessage.Id);
                            _logger.LogWarning("Falha ao enviar email {Id}: {Message}", emailMessage.Id, result.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        await _rabbitMQService.NackEmailMessageAsync(emailMessage.Id);
                        _logger.LogError(ex, "Erro ao processar email {Id}", emailMessage.Id);
                    }
                }

                _logger.LogInformation("Processamento concluído. Processados: {Processed}, Sucessos: {Success}",
                    processedCount, successCount);

            }, nameof(ProcessEmailQueueAsync));
        }

        [Queue("email")]
        public async Task SendEmailAsync(string emailData, string correlationId)
        {
            await ExecuteWithErrorHandling(async () =>
            {
                _logger.LogInformation("Enviando email. CorrelationId: {CorrelationId}", correlationId);

                EmailMessage emailMessage;
                try
                {
                    emailMessage = System.Text.Json.JsonSerializer.Deserialize<EmailMessage>(emailData);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao desserializar dados do email. CorrelationId: {CorrelationId}", correlationId);
                    throw new InvalidOperationException("Dados inválidos para envio de email.");
                }

                var result = await _emailService.SendEmailAsync(emailMessage);

                if (!result.Success)
                {
                    throw new InvalidOperationException($"Falha no envio de email: {result.Message}");
                }

            }, nameof(SendEmailAsync));
        }

        [Queue("email")]
        public async Task SendReportEmailAsync(string reportType, List<string> recipients, string correlationId)
        {
            await ExecuteWithErrorHandling(async () =>
            {
                _logger.LogInformation("Enviando relatório {ReportType} para {Count} destinatários",
                    reportType, recipients.Count);

                var result = await _emailService.SendReportEmailAsync(reportType, recipients, correlationId);

                if (!result.Success)
                {
                    throw new InvalidOperationException($"Falha no envio de relatório: {result.Message}");
                }

            }, nameof(SendReportEmailAsync));
        }
    }
}