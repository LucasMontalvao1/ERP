using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.Models.DTOs.SalesForce
{
    /// <summary>
    /// DTO para resposta de autenticação do SalesForce
    /// </summary>
    public class SalesForceAuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = "Bearer";
        public DateTime ExpiresAt { get; set; }
        public int ExpiresIn { get; set; }
        public string Scope { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para teste de autenticação
    /// </summary>
    public class SalesForceAuthTestDto
    {
        public bool Authenticated { get; set; }
        public DateTime? TokenExpiration { get; set; }
        public string? ApiVersion { get; set; }
        public int ResponseTime { get; set; }
        public string? BaseUrl { get; set; }
        public DateTime TestedAt { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// DTO para verificação de saúde da API
    /// </summary>
    public class SalesForceHealthDto
    {
        public bool IsHealthy { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ResponseTime { get; set; }
        public string? Version { get; set; }
        public DateTime CheckedAt { get; set; }
        public List<string> Issues { get; set; } = new();
    }

    /// <summary>
    /// DTO para teste completo de integração
    /// </summary>
    public class SalesForceIntegrationTestDto
    {
        public bool AuthenticationSuccess { get; set; }
        public bool DataSendSuccess { get; set; }
        public int TotalResponseTime { get; set; }
        public string? TestDataSent { get; set; }
        public string? ResponseReceived { get; set; }
        public DateTime TestedAt { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// DTO para dados de teste
    /// </summary>
    public class SalesForceTestDataDto
    {
        [Required]
        public string CodAtiv { get; set; } = string.Empty;

        [Required]
        public string Ramo { get; set; } = string.Empty;

        public decimal PercDesc { get; set; }

        public string CalculaSt { get; set; } = "N";
    }

    /// <summary>
    /// DTO para configurações do SalesForce
    /// </summary>
    public class SalesForceConfigDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string VersaoApi { get; set; } = string.Empty;
        public bool Ativo { get; set; }
        public int TimeoutSegundos { get; set; }
        public DateTime? UltimaConexao { get; set; }
        public string? Status { get; set; }
    }

    /// <summary>
    /// DTO para validação de configuração
    /// </summary>
    public class SalesForceConfigValidationDto
    {
        public bool IsValid { get; set; }
        public bool ConnectionSuccessful { get; set; }
        public bool AuthenticationSuccessful { get; set; }
        public int ResponseTime { get; set; }
        public DateTime ValidatedAt { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public string CorrelationId { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para métricas de performance
    /// </summary>
    public class SalesForceMetricsDto
    {
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public double SuccessRate { get; set; }
        public int AverageResponseTime { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public List<SalesForceMetricDetail> Details { get; set; } = new();
    }

    public class SalesForceMetricDetail
    {
        public DateTime Date { get; set; }
        public int Requests { get; set; }
        public int Successes { get; set; }
        public int Failures { get; set; }
        public int AvgResponseTime { get; set; }
    }

    /// <summary>
    /// DTO para resultado de sincronização
    /// </summary>
    public class SalesForceSyncResult
    {
        public bool Success { get; set; }
        public string? ExternalId { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ResponseTime { get; set; }
        public int HttpStatusCode { get; set; }
        public DateTime ProcessedAt { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public object? ResponseData { get; set; }
    }

    /// <summary>
    /// DTO para resultado de sincronização de lote
    /// </summary>
    public class SalesForceSyncResultDto
    {
        public int TotalProcessed { get; set; }
        public int Successful { get; set; }
        public int Failed { get; set; }
        public List<SalesForceSyncResult> Results { get; set; } = new();
        public DateTime ProcessedAt { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para resposta genérica da API SalesForce
    /// </summary>
    public class SalesForceApiResponse
    {
        public bool Success { get; set; }
        public object? Data { get; set; }
        public string Message { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public string? ErrorCode { get; set; }
        public List<string> Errors { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// DTO para request de login
    /// </summary>
    public class SalesForceLoginRequest
    {
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string GrantType { get; set; } = "password";
    }

    /// <summary>
    /// DTO para dados de atividade enviados ao SalesForce
    /// </summary>
    public class SalesForceAtividadeDto
    {
        public string CodAtiv { get; set; } = string.Empty;
        public string Ramo { get; set; } = string.Empty;
        public decimal PercDesc { get; set; }
        public string CalculaSt { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; }
        public DateTime? DataAtualizacao { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public string SourceSystem { get; set; } = "ERP_MONTALVAO";
    }

    /// <summary>
    /// Modelo para resposta da API externa de atividades
    /// </summary>
    public class ExternalApiAtividadeResponse
    {
        public List<ExternalApiSuccess> Success { get; set; } = new();
        public List<ExternalApiError> Errors { get; set; } = new();
    }

    public class ExternalApiSuccess
    {
        public ExternalApiChave Chave { get; set; } = new();
    }

    public class ExternalApiChave
    {
        public string CodAtiv { get; set; } = string.Empty;
    }

    public class ExternalApiError
    {
        public string Message { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    /// <summary>
    /// Modelo para requisição da API externa de atividades
    /// </summary>
    public class ExternalApiAtividadeRequest
    {
        [JsonPropertyName("codativ")]
        public string CodAtiv { get; set; } = string.Empty;

        [JsonPropertyName("percdesc")]
        public decimal PercDesc { get; set; }

        [JsonPropertyName("hash")]
        public string Hash { get; set; } = string.Empty;

        [JsonPropertyName("ramo")]
        public string Ramo { get; set; } = string.Empty;

        [JsonPropertyName("calculast")]
        public string CalculaSt { get; set; } = string.Empty;
    }
}