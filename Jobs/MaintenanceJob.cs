using API.Jobs.Base;
using API.Services.Interfaces;
using Hangfire;

namespace API.Jobs
{
    /// <summary>
    /// Job para tarefas de manutenção
    /// </summary>
    public class MaintenanceJob : BaseJob
    {
        private readonly IIntegrationService _integrationService;
        private readonly IEmailService _emailService;

        public MaintenanceJob(
            IIntegrationService integrationService,
            IEmailService emailService,
            ILogger<MaintenanceJob> logger) : base(logger)
        {
            _integrationService = integrationService;
            _emailService = emailService;
        }

        /// <summary>
        /// Executa todas as tarefas de manutenção
        /// </summary>
        [Queue("maintenance")]
        public async Task RunMaintenanceTasksAsync()
        {
            await ExecuteWithErrorHandling(async () =>
            {
                _logger.LogInformation("Iniciando tarefas de manutenção");

                await CleanupOldLogsAsync();
                await CleanupExpiredCacheAsync();
                await CheckDataIntegrityAsync();
                await OptimizeDatabaseAsync();

                _logger.LogInformation("Tarefas de manutenção concluídas");

            }, nameof(RunMaintenanceTasksAsync));
        }

        private async Task CleanupOldLogsAsync()
        {
            _logger.LogInformation("Iniciando limpeza de logs antigos");
            var deletedCount = await _integrationService.CleanupOldLogsAsync(TimeSpan.FromDays(30));
            _logger.LogInformation("Limpeza de logs concluída. Registros removidos: {Count}", deletedCount);
        }

        private async Task CleanupExpiredCacheAsync()
        {
            _logger.LogInformation("Iniciando limpeza de cache expirado");
            await _integrationService.CleanupExpiredCacheAsync();
            _logger.LogInformation("Limpeza de cache concluída");
        }

        private async Task CheckDataIntegrityAsync()
        {
            _logger.LogInformation("Verificando integridade dos dados");
            var issues = await _integrationService.CheckDataIntegrityAsync();

            if (issues.Any())
            {
                _logger.LogWarning("Encontrados {Count} problemas de integridade", issues.Count);
                await _emailService.SendDataIntegrityReportAsync(issues);
            }
            else
            {
                _logger.LogInformation("Verificação de integridade concluída sem problemas");
            }
        }

        private async Task OptimizeDatabaseAsync()
        {
            _logger.LogInformation("Iniciando otimização do banco de dados");
            await _integrationService.OptimizeDatabaseAsync();
            _logger.LogInformation("Otimização do banco concluída");
        }
    }
}
