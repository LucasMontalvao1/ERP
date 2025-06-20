using API.Jobs.Base;
using API.Services.Interfaces;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace API.Jobs
{
    /// <summary>
    /// Job para processamento de integrações
    /// </summary>
    public class IntegrationJob : BaseJob
    {
        private readonly IIntegrationService _integrationService;
        private readonly IAtividadeService _atividadeService;

        public IntegrationJob(
            IIntegrationService integrationService,
            IAtividadeService atividadeService,
            ILogger<IntegrationJob> logger) : base(logger)
        {
            _integrationService = integrationService;
            _atividadeService = atividadeService;
        }

        [Queue("integration")]
        public async Task ProcessFailedIntegrationsAsync()
        {
            await ExecuteWithErrorHandling(async () =>
            {
                var failedIntegrations = await _integrationService.GetFailedIntegrationsAsync();

                _logger.LogInformation("Encontradas {Count} integrações falhadas para reprocessamento",
                    failedIntegrations.Count);

                var processedCount = 0;
                var successCount = 0;

                foreach (var integration in failedIntegrations)
                {
                    try
                    {
                        var result = await _integrationService.RetryIntegrationAsync(integration.Id);
                        processedCount++;

                        if (result.Success)
                        {
                            successCount++;
                            _logger.LogInformation("Integração {Id} reprocessada com sucesso", integration.Id);
                        }
                        else
                        {
                            _logger.LogWarning("Falha ao reprocessar integração {Id}: {Message}",
                                integration.Id, result.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao reprocessar integração {Id}", integration.Id);
                    }
                }

                _logger.LogInformation("Reprocessamento concluído. Processadas: {Processed}, Sucessos: {Success}",
                    processedCount, successCount);

            }, nameof(ProcessFailedIntegrationsAsync));
        }

        [Queue("integration")]
        public async Task ProcessAtividadeIntegrationAsync(int atividadeId, string correlationId)
        {
            await ExecuteWithErrorHandling(async () =>
            {
                _logger.LogInformation("Processando integração da atividade {AtividadeId}. CorrelationId: {CorrelationId}",
                    atividadeId, correlationId);

                var result = await _integrationService.ProcessAtividadeAsync(atividadeId.ToString(), correlationId);

                if (result.Success)
                {
                    _logger.LogInformation("Atividade {AtividadeId} integrada com sucesso", atividadeId);
                }
                else
                {
                    _logger.LogError("Falha na integração da atividade {AtividadeId}: {Message}",
                        atividadeId, result.Message);
                    throw new InvalidOperationException($"Falha na integração: {result.Message}");
                }
            }, nameof(ProcessAtividadeIntegrationAsync));
        }

        [Queue("integration")]
        public async Task SyncBatchDataAsync(List<int> atividadeIds, string correlationId)
        {
            await ExecuteWithErrorHandling(async () =>
            {
                _logger.LogInformation("Iniciando sincronização em lote de {Count} atividades. CorrelationId: {CorrelationId}",
                    atividadeIds.Count, correlationId);

                var atividadeIdsAsString = atividadeIds.Select(id => id.ToString()).ToList();

                var result = await _integrationService.ProcessBatchAsync(atividadeIdsAsString, correlationId);

                if (result.Success && result.Data != null)
                {
                    _logger.LogInformation("Sincronização em lote concluída. Sucessos: {Success}, Falhas: {Failures}",
                        result.Data.SuccessCount, result.Data.FailureCount);
                }
                else
                {
                    _logger.LogWarning("Sincronização em lote falhou. Mensagem: {Message}", result.Message);
                }
            }, nameof(SyncBatchDataAsync));
        }
    }
}