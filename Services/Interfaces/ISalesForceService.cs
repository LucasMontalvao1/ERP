using API.Models.DTOs.SalesForce;
using API.Models.Responses;

namespace API.Services.Interfaces
{
    public interface ISalesForceService
    {
        /// <summary>
        /// Autenticar na API do SalesForce
        /// </summary>
        /// <returns>Resultado da autenticação com dados do token</returns>
        Task<SalesForceServiceResult<SalesForceAuthDto>> AuthenticateAsync();

        /// <summary>
        /// Verificar saúde da API do SalesForce
        /// </summary>
        /// <returns>Status de saúde da API</returns>
        Task<SalesForceHealthDto> CheckHealthAsync();

        /// <summary>
        /// Obter URL base configurada
        /// </summary>
        /// <returns>URL base da API</returns>
        Task<string> GetBaseUrlAsync();

        /// <summary>
        /// Limpar cache de autenticação
        /// </summary>
        Task ClearAuthCacheAsync();

        /// <summary>
        /// Validar se o token atual é válido
        /// </summary>
        /// <returns>True se o token é válido</returns>
        Task<bool> ValidateTokenAsync();

        // ✅ NOVOS MÉTODOS ADICIONADOS
        /// <summary>
        /// Criar nova atividade na API externa
        /// </summary>
        /// <param name="atividade">Dados da atividade</param>
        /// <param name="correlationId">ID de correlação para rastreamento</param>
        /// <returns>Resultado da criação</returns>
        Task<SalesForceResult<AtividadeIntegrationResponseDto>> CreateAtividadeAsync(
            Models.DTOs.Atividade.AtividadeSyncDto atividade,
            string correlationId);

        /// <summary>
        /// Atualizar atividade existente na API externa
        /// </summary>
        /// <param name="atividade">Dados da atividade</param>
        /// <param name="correlationId">ID de correlação para rastreamento</param>
        /// <returns>Resultado da atualização</returns>
        Task<SalesForceResult<AtividadeIntegrationResponseDto>> UpdateAtividadeAsync(
            Models.DTOs.Atividade.AtividadeSyncDto atividade,
            string correlationId);

        // ⚠️ MÉTODOS ANTIGOS - MANTER PARA COMPATIBILIDADE
        /// <summary>
        /// Enviar atividade para o SalesForce (método legado)
        /// </summary>
        /// <param name="atividade">Dados da atividade</param>
        /// <param name="correlationId">ID de correlação para rastreamento</param>
        /// <returns>Resultado do envio</returns>
        [Obsolete("Use CreateAtividadeAsync para novas implementações")]
        Task<ApiResponse<SalesForceSyncResult>> SendAtividadeAsync(
            Models.DTOs.Atividade.AtividadeSyncDto atividade,
            string correlationId);

        /// <summary>
        /// Deletar atividade no SalesForce
        /// </summary>
        /// <param name="codAtiv">Código da atividade</param>
        /// <param name="correlationId">ID de correlação para rastreamento</param>
        /// <returns>Resultado da exclusão</returns>
        Task<ApiResponse<SalesForceSyncResult>> DeleteAtividadeAsync(
            string codAtiv,
            string correlationId);

        /// <summary>
        /// Enviar lote de atividades para o SalesForce
        /// </summary>
        /// <param name="atividades">Lista de atividades</param>
        /// <param name="correlationId">ID de correlação para rastreamento</param>
        /// <returns>Resultado do envio em lote</returns>
        Task<ApiResponse<List<SalesForceSyncResult>>> SendBatchAsync(
            List<Models.DTOs.Atividade.AtividadeSyncDto> atividades,
            string correlationId);

        /// <summary>
        /// Executar requisição genérica para a API
        /// </summary>
        /// <param name="endpoint">Endpoint da requisição</param>
        /// <param name="method">Método HTTP</param>
        /// <param name="data">Dados para envio (opcional)</param>
        /// <param name="correlationId">ID de correlação (opcional)</param>
        /// <returns>Resposta da API</returns>
        Task<ApiResponse<SalesForceApiResponse>> ExecuteRequestAsync(
            string endpoint,
            HttpMethod method,
            object? data = null,
            string? correlationId = null);

        /// <summary>
        /// Enviar dados de teste para a API externa no formato correto
        /// </summary>
        /// <param name="testData">Dados de teste para envio</param>
        /// <param name="correlationId">ID de correlação para rastreamento</param>
        /// <returns>Resultado do envio com resposta da API externa estruturada</returns>
        Task<ApiResponse<ExternalApiAtividadeResponse>> SendTestDataAsync(
            SalesForceTestDataDto testData,
            string correlationId);
    }
}