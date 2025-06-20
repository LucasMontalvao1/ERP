using API.Jobs.Base;
using API.Models.DTOs.Webhook;
using API.Services.Interfaces;
using Hangfire;

namespace API.Jobs
{
    /// <summary>
    /// Job para geração de relatórios
    /// </summary>
    public class ReportJob : BaseJob
    {
        private readonly IIntegrationService _integrationService;
        private readonly IEmailService _emailService;

        public ReportJob(
            IIntegrationService integrationService,
            IEmailService emailService,
            ILogger<ReportJob> logger) : base(logger)
        {
            _integrationService = integrationService;
            _emailService = emailService;
        }

        /// <summary>
        /// Gera relatórios semanais
        /// </summary>
        [Queue("reports")]
        public async Task GenerateWeeklyReportsAsync()
        {
            await ExecuteWithErrorHandling(async () =>
            {
                _logger.LogInformation("Gerando relatórios semanais");

                await GenerateIntegrationReportAsync();
                await GeneratePerformanceReportAsync();
                await GenerateErrorReportAsync();

                _logger.LogInformation("Geração de relatórios semanais concluída");

            }, nameof(GenerateWeeklyReportsAsync));
        }

        /// <summary>
        /// Gera relatório customizado
        /// </summary>
        [Queue("reports")]
        public async Task GenerateCustomReportAsync(string reportConfig, string correlationId)
        {
            await ExecuteWithErrorHandling(async () =>
            {
                _logger.LogInformation("Gerando relatório customizado. CorrelationId: {CorrelationId}", correlationId);

                var config = System.Text.Json.JsonSerializer.Deserialize<ReportConfig>(reportConfig);
                var result = await _integrationService.GenerateCustomReportAsync(config, correlationId);

                if (!result.Success)
                {
                    throw new InvalidOperationException($"Falha na geração de relatório: {result.Message}");
                }

            }, $"GenerateCustomReport-{correlationId}");
        }

        private async Task GenerateIntegrationReportAsync()
        {
            var report = await _integrationService.GenerateIntegrationReportAsync(TimeSpan.FromDays(7));
            var recipients = await GetReportRecipientsAsync("integration");

            var reportContent = System.Text.Json.JsonSerializer.Serialize(report);
            await _emailService.SendReportEmailAsync("Relatório Semanal de Integrações", recipients, reportContent);
        }

        private async Task GeneratePerformanceReportAsync()
        {
            var report = await _integrationService.GeneratePerformanceReportAsync(TimeSpan.FromDays(7));
            var recipients = await GetReportRecipientsAsync("performance");

            var reportContent = System.Text.Json.JsonSerializer.Serialize(report);
            await _emailService.SendReportEmailAsync("Relatório Semanal de Performance", recipients, reportContent);
        }

        private async Task GenerateErrorReportAsync()
        {
            var report = await _integrationService.GenerateErrorReportAsync(TimeSpan.FromDays(7));
            var recipients = await GetReportRecipientsAsync("errors");

            var reportContent = System.Text.Json.JsonSerializer.Serialize(report);
            await _emailService.SendReportEmailAsync("Relatório Semanal de Erros", recipients, reportContent);
        }

        private async Task<List<string>> GetReportRecipientsAsync(string reportType)
        {
            return await _integrationService.GetReportRecipientsAsync(reportType);
        }
    }
}
