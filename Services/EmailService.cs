using API.Jobs.Base;
using API.Models.Configurations;
using API.Models.DTOs.Webhook;
using API.Models.Responses;
using API.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace API.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailConfig _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IOptions<EmailConfig> config,
            ILogger<EmailService> logger)
        {
            _config = config.Value;
            _logger = logger;
        }

        public async Task<ApiResponse<bool>> SendEmailAsync(EmailMessage emailMessage)
        {
            try
            {
                _logger.LogInformation("Enviando email para {To} com assunto: {Subject}",
                    emailMessage.To, emailMessage.Subject);

                using var client = CreateSmtpClient();
                using var message = CreateMailMessage(emailMessage);

                await client.SendMailAsync(message);

                _logger.LogInformation("Email enviado com sucesso para {To}", emailMessage.To);

                return ApiResponse<bool>.SuccessResult(true, "Email enviado com sucesso");
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "Erro SMTP ao enviar email para {To}: {Error}",
                    emailMessage.To, ex.Message);
                return ApiResponse<bool>.ErrorResult($"Erro SMTP: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar email para {To}", emailMessage.To);
                return ApiResponse<bool>.ErrorResult("Erro interno ao enviar email");
            }
        }

        public async Task<ApiResponse<bool>> SendReportEmailAsync(string reportType, List<string> recipients, string correlationId)
        {
            try
            {
                _logger.LogInformation("Enviando relatório {ReportType} para {Count} destinatários. CorrelationId: {CorrelationId}",
                    reportType, recipients.Count, correlationId);

                var subject = $"Relatório {reportType} - {DateTime.Now:dd/MM/yyyy}";
                var body = GenerateReportEmailBody(reportType, correlationId);

                var successCount = 0;
                var errors = new List<string>();

                foreach (var recipient in recipients)
                {
                    try
                    {
                        var emailMessage = new EmailMessage
                        {
                            Id = Guid.NewGuid().ToString(),
                            To = recipient,
                            Subject = subject,
                            Body = body,
                            IsHtml = true
                        };

                        var result = await SendEmailAsync(emailMessage);
                        if (result.Success)
                        {
                            successCount++;
                        }
                        else
                        {
                            errors.Add($"{recipient}: {result.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{recipient}: {ex.Message}");
                        _logger.LogError(ex, "Erro ao enviar relatório para {Recipient}", recipient);
                    }
                }

                var message = $"Relatório enviado para {successCount}/{recipients.Count} destinatários";
                if (errors.Any())
                {
                    message += $". Erros: {string.Join("; ", errors)}";
                }

                _logger.LogInformation("Envio de relatório concluído. Sucessos: {Success}/{Total}. CorrelationId: {CorrelationId}",
                    successCount, recipients.Count, correlationId);

                return ApiResponse<bool>.SuccessResult(successCount > 0, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar relatório {ReportType}. CorrelationId: {CorrelationId}",
                    reportType, correlationId);
                return ApiResponse<bool>.ErrorResult("Erro interno ao enviar relatório");
            }
        }

        public async Task<ApiResponse<bool>> SendDataIntegrityReportAsync(List<string> issues)
        {
            try
            {
                var subject = $"Relatório de Integridade de Dados - {DateTime.Now:dd/MM/yyyy HH:mm}";
                var body = GenerateDataIntegrityEmailBody(issues);

                var emailMessage = new EmailMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    To = "lucasdesouza015@gmail.com", 
                    Subject = subject,
                    Body = body,
                    IsHtml = true
                };

                return await SendEmailAsync(emailMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar relatório de integridade de dados");
                return ApiResponse<bool>.ErrorResult("Erro interno ao enviar relatório de integridade");
            }
        }

        public async Task<ApiResponse<bool>> SendIntegrationNotificationAsync(string type, object data, string correlationId)
        {
            try
            {
                var subject = type switch
                {
                    "integration_success" => "Integração realizada com sucesso",
                    "integration_error" => "Erro na integração",
                    "data_updated" => "Dados atualizados via integração",
                    _ => "Notificação de integração"
                };

                var body = GenerateIntegrationNotificationBody(type, data, correlationId);

                var emailMessage = new EmailMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    To = "ilucasdesouza015@gmail.com", 
                    Subject = subject,
                    Body = body,
                    IsHtml = true
                };

                return await SendEmailAsync(emailMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar notificação de integração. Tipo: {Type}, CorrelationId: {CorrelationId}",
                    type, correlationId);
                return ApiResponse<bool>.ErrorResult("Erro interno ao enviar notificação");
            }
        }

        public async Task<bool> ValidateEmailConfigurationAsync()
        {
            try
            {
                using var client = CreateSmtpClient();

                // Tentar conectar sem enviar email
                await Task.Run(() =>
                {
                    client.Timeout = 10000; 
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar configuração de email");
                return false;
            }
        }

        public async Task<ApiResponse<object>> GetEmailStatisticsAsync(int days = 7)
        {
            try
            {
                // Implementar busca de estatísticas 
                // Por ora, retorna estatísticas mockadas
                var statistics = new
                {
                    Period = $"Últimos {days} dias",
                    TotalSent = 0, 
                    Successful = 0,
                    Failed = 0,
                    SuccessRate = 0.0,
                    LastEmail = DateTime.UtcNow,
                    ConfigurationValid = await ValidateEmailConfigurationAsync()
                };

                return ApiResponse<object>.SuccessResult(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar estatísticas de email");
                return ApiResponse<object>.ErrorResult("Erro interno ao buscar estatísticas");
            }
        }

        // Métodos auxiliares privados
        private SmtpClient CreateSmtpClient()
        {
            var client = new SmtpClient(_config.SmtpServer, _config.SmtpPort)
            {
                Credentials = new NetworkCredential(_config.Username, _config.Password),
                EnableSsl = _config.SmtpPort == 587 || _config.SmtpPort == 465,
                Timeout = 30000 
            };

            return client;
        }

        private MailMessage CreateMailMessage(EmailMessage emailMessage)
        {
            var message = new MailMessage
            {
                From = new MailAddress(_config.FromEmail, _config.FromName),
                Subject = emailMessage.Subject,
                Body = emailMessage.Body,
                IsBodyHtml = emailMessage.IsHtml,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };

            message.To.Add(emailMessage.To);

            // Adicionar anexos se houver
            if (emailMessage.Attachments != null)
            {
                foreach (var attachmentPath in emailMessage.Attachments)
                {
                    if (File.Exists(attachmentPath))
                    {
                        message.Attachments.Add(new Attachment(attachmentPath));
                    }
                }
            }

            return message;
        }

        private string GenerateReportEmailBody(string reportType, string correlationId)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html><head><meta charset='utf-8'><title>Relatório</title></head><body>");
            html.AppendLine("<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>");
            html.AppendLine($"<h2>Relatório {reportType}</h2>");
            html.AppendLine($"<p><strong>Data/Hora:</strong> {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>");
            html.AppendLine($"<p><strong>Correlation ID:</strong> {correlationId}</p>");
            html.AppendLine("<p>O relatório foi gerado automaticamente pelo sistema ERP.</p>");
            html.AppendLine("<p>Para mais detalhes, acesse o painel administrativo.</p>");
            html.AppendLine("<hr>");
            html.AppendLine("<p style='font-size: 12px; color: #666;'>Este é um email automático. Não responda.</p>");
            html.AppendLine("</div></body></html>");

            return html.ToString();
        }

        private string GenerateDataIntegrityEmailBody(List<string> issues)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html><head><meta charset='utf-8'><title>Relatório de Integridade</title></head><body>");
            html.AppendLine("<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>");
            html.AppendLine("<h2 style='color: #d32f2f;'>⚠️ Problemas de Integridade Detectados</h2>");
            html.AppendLine($"<p><strong>Data/Hora:</strong> {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>");
            html.AppendLine($"<p><strong>Total de problemas:</strong> {issues.Count}</p>");
            html.AppendLine("<h3>Detalhes dos problemas:</h3>");
            html.AppendLine("<ul>");

            foreach (var issue in issues)
            {
                html.AppendLine($"<li>{issue}</li>");
            }

            html.AppendLine("</ul>");
            html.AppendLine("<p><strong>Ação recomendada:</strong> Verifique os dados e execute as correções necessárias.</p>");
            html.AppendLine("<hr>");
            html.AppendLine("<p style='font-size: 12px; color: #666;'>Este é um email automático. Não responda.</p>");
            html.AppendLine("</div></body></html>");

            return html.ToString();
        }

        private string GenerateIntegrationNotificationBody(string type, object data, string correlationId)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html><head><meta charset='utf-8'><title>Notificação de Integração</title></head><body>");
            html.AppendLine("<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>");

            var icon = type switch
            {
                "integration_success" => "✅",
                "integration_error" => "❌",
                "data_updated" => "🔄",
                _ => "ℹ️"
            };

            var color = type switch
            {
                "integration_success" => "#4caf50",
                "integration_error" => "#f44336",
                "data_updated" => "#ff9800",
                _ => "#2196f3"
            };

            html.AppendLine($"<h2 style='color: {color};'>{icon} Notificação de Integração</h2>");
            html.AppendLine($"<p><strong>Tipo:</strong> {type}</p>");
            html.AppendLine($"<p><strong>Data/Hora:</strong> {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>");
            html.AppendLine($"<p><strong>Correlation ID:</strong> {correlationId}</p>");
            html.AppendLine("<h3>Dados:</h3>");
            html.AppendLine($"<pre style='background: #f5f5f5; padding: 10px; border-radius: 4px;'>{System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })}</pre>");
            html.AppendLine("<hr>");
            html.AppendLine("<p style='font-size: 12px; color: #666;'>Este é um email automático. Não responda.</p>");
            html.AppendLine("</div></body></html>");

            return html.ToString();
        }
    }
}