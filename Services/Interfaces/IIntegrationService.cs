using API.Models.DTOs.Integration;
using API.Models.DTOs.SalesForce;
using API.Models.DTOs.Webhook;
using API.Models.Responses;

namespace API.Services.Interfaces
{
    public interface IIntegrationService
    {
        // Processamento de atividades
        /// <summary>
        /// Processar atividade para integração com API externa
        /// </summary>
        /// <param name="codAtiv">Código da atividade</param>
        /// <param name="correlationId">ID de correlação</param>
        /// <param name="isNewActivity">Indica se é uma nova atividade (CREATE) ou atualização (UPDATE)</param>
        /// <returns>Resultado do processamento</returns>
        Task<ApiResponse<IntegrationResultDto>> ProcessAtividadeAsync(string codAtiv, string correlationId, bool isNewActivity = false);

        Task<ApiResponse<BatchProcessResultDto>> ProcessBatchAsync(List<string> codAtivs, string correlationId);
        Task<ApiResponse<IntegrationResultDto>> RetryIntegrationAsync(int logId);

        // Gerenciamento de integrações falhadas
        Task<List<Models.Entities.LogSincronizacao>> GetFailedIntegrationsAsync();
        Task<ApiResponse<IntegrationResultDto>> RetryIntegrationAsync(string codAtiv);

        // Configurações
        Task<List<SalesForceConfigDto>> GetActiveConfigurationsAsync();
        Task<ApiResponse<SalesForceConfigValidationDto>> ValidateConfigurationAsync(int configId, string correlationId);

        // Métricas e estatísticas
        Task<SalesForceMetricsDto> GetIntegrationMetricsAsync(int days);
        Task<ApiResponse<IntegrationStatisticsDto>> GetIntegrationStatisticsAsync(int days = 7);

        // Sincronização forçada
        Task<ApiResponse<SalesForceSyncResultDto>> ForceSyncPendingDataAsync(string correlationId);

        // Teste de integração completa
        Task<ApiResponse<SalesForceIntegrationTestDto>> TestCompleteIntegrationAsync(SalesForceTestDataDto testData, string correlationId);

        // Webhooks
        Task LogWebhookAsync(WebhookLogCreateDto webhookLog);
        Task<PagedResponse<WebhookLogDto>> GetWebhookLogsAsync(WebhookLogFilterDto filter);
        Task<ApiResponse<WebhookLogDetailDto>> GetWebhookLogByIdAsync(int logId);
        Task<ApiResponse<bool>> ReprocessWebhookAsync(int logId, string correlationId);
        Task<WebhookStatisticsDto> GetWebhookStatisticsAsync(int days);
        Task<ApiResponse<WebhookTestResultDto>> TestWebhookAsync(WebhookTestDto testData, string correlationId);

        // Configurações de webhook
        Task<ApiResponse<WebhookConfigDto>> CreateWebhookConfigAsync(CreateWebhookConfigDto config);
        Task<ApiResponse<WebhookConfigDto>> GetWebhookConfigAsync(int configId);
        Task<List<WebhookConfigDto>> GetWebhookConfigsAsync();
        Task<ApiResponse<WebhookConfigDto>> UpdateWebhookConfigAsync(int configId, UpdateWebhookConfigDto config);
        Task<ApiResponse<bool>> DeleteWebhookConfigAsync(int configId);

        // Status de integração
        Task<bool> UpdateIntegrationStatusAsync(int entityId, int status, string? externalId, string correlationId, string? errorMessage = null);

        // Sincronização de dados externos
        Task<ApiResponse<bool>> SyncDataFromExternalAsync(int entityId, string externalId, object? data, string correlationId);

        // Manutenção e limpeza
        Task<int> CleanupOldLogsAsync(TimeSpan maxAge);
        Task CleanupExpiredCacheAsync();
        Task<List<string>> CheckDataIntegrityAsync();
        Task OptimizeDatabaseAsync();

        // Relatórios
        Task<object> GenerateIntegrationReportAsync(TimeSpan period);
        Task<object> GeneratePerformanceReportAsync(TimeSpan period);
        Task<object> GenerateErrorReportAsync(TimeSpan period);
        Task<List<string>> GetReportRecipientsAsync(string reportType);
        Task<ApiResponse<object>> GenerateCustomReportAsync(object config, string correlationId);

        // Monitoramento
        Task<IntegrationMonitorDto> GetIntegrationMonitorAsync();
    }
}