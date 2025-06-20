using System.ComponentModel.DataAnnotations;

namespace API.Models.DTOs.Webhook
{
    /// <summary>
    /// DTO para webhook do SalesForce
    /// </summary>
    public class SalesForceWebhookDto
    {
        public string? EventType { get; set; }
        public int? EntityId { get; set; }
        public string? ExternalId { get; set; }
        public DateTime Timestamp { get; set; }
        public object? Data { get; set; }
        public string? Source { get; set; } = "SalesForce";
    }

    /// <summary>
    /// DTO para webhook genérico
    /// </summary>
    public class GenericWebhookDto
    {
        [Required]
        public string Source { get; set; } = string.Empty;

        [Required]
        public string EventType { get; set; } = string.Empty;

        public object? Data { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? CorrelationId { get; set; }
    }

    /// <summary>
    /// DTO para log de webhook
    /// </summary>
    public class WebhookLogDto
    {
        public int Id { get; set; }
        public string Source { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; }
        public bool ProcessedSuccessfully { get; set; }
        public string? ErrorMessage { get; set; }
        public int ResponseTime { get; set; }
        public string? CorrelationId { get; set; }
    }

    /// <summary>
    /// DTO para detalhes do log de webhook
    /// </summary>
    public class WebhookLogDetailDto
    {
        public int Id { get; set; }
        public string Source { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public string? Response { get; set; }
        public DateTime ReceivedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public bool ProcessedSuccessfully { get; set; }
        public string? ErrorMessage { get; set; }
        public int ResponseTime { get; set; }
        public string? CorrelationId { get; set; }
    }

    /// <summary>
    /// DTO para filtro de logs de webhook
    /// </summary>
    public class WebhookLogFilterDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Source { get; set; }
        public string? EventType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? ProcessedSuccessfully { get; set; }
    }

    /// <summary>
    /// DTO para estatísticas de webhooks
    /// </summary>
    public class WebhookStatisticsDto
    {
        public int TotalReceived { get; set; }
        public int ProcessedSuccessfully { get; set; }
        public int ProcessedWithErrors { get; set; }
        public double SuccessRate { get; set; }
        public int AverageResponseTime { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public List<WebhookStatisticBySource> BySource { get; set; } = new();
        public List<WebhookStatisticDaily> Daily { get; set; } = new();
    }

    public class WebhookStatisticBySource
    {
        public string Source { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Successful { get; set; }
        public int Failed { get; set; }
        public double SuccessRate { get; set; }
    }

    public class WebhookStatisticDaily
    {
        public DateTime Date { get; set; }
        public int Total { get; set; }
        public int Successful { get; set; }
        public int Failed { get; set; }
    }

    /// <summary>
    /// DTO para teste de webhook
    /// </summary>
    public class WebhookTestDto
    {
        [Required]
        public string Source { get; set; } = string.Empty;

        [Required]
        public string EventType { get; set; } = string.Empty;

        [Required]
        public object TestData { get; set; } = new();

        public bool SimulateError { get; set; } = false;
    }

    /// <summary>
    /// DTO para resultado de teste de webhook
    /// </summary>
    public class WebhookTestResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ResponseTime { get; set; }
        public DateTime TestedAt { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public object? ProcessedData { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// DTO para configuração de webhook
    /// </summary>
    public class WebhookConfigDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string UrlWebhook { get; set; } = string.Empty;
        public List<string> Eventos { get; set; } = new();
        public Dictionary<string, string>? HeadersCustomizados { get; set; }
        public int TimeoutSegundos { get; set; }
        public int MaxTentativas { get; set; }
        public bool Ativo { get; set; }
        public DateTime DataCriacao { get; set; }
    }

    /// <summary>
    /// DTO para criar configuração de webhook
    /// </summary>
    public class CreateWebhookConfigDto
    {
        [Required(ErrorMessage = "Nome é obrigatório")]
        [StringLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "URL do webhook é obrigatória")]
        [Url(ErrorMessage = "URL inválida")]
        [StringLength(500, ErrorMessage = "URL deve ter no máximo 500 caracteres")]
        public string UrlWebhook { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pelo menos um evento deve ser especificado")]
        public List<string> Eventos { get; set; } = new();

        public Dictionary<string, string>? HeadersCustomizados { get; set; }

        [Range(5, 60, ErrorMessage = "Timeout deve estar entre 5 e 60 segundos")]
        public int TimeoutSegundos { get; set; } = 10;

        [Range(1, 5, ErrorMessage = "Máximo de tentativas deve estar entre 1 e 5")]
        public int MaxTentativas { get; set; } = 3;

        public bool Ativo { get; set; } = true;
    }

    /// <summary>
    /// DTO para atualizar configuração de webhook
    /// </summary>
    public class UpdateWebhookConfigDto
    {
        [Required(ErrorMessage = "Nome é obrigatório")]
        [StringLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "URL do webhook é obrigatória")]
        [Url(ErrorMessage = "URL inválida")]
        [StringLength(500, ErrorMessage = "URL deve ter no máximo 500 caracteres")]
        public string UrlWebhook { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pelo menos um evento deve ser especificado")]
        public List<string> Eventos { get; set; } = new();

        public Dictionary<string, string>? HeadersCustomizados { get; set; }

        [Range(5, 60, ErrorMessage = "Timeout deve estar entre 5 e 60 segundos")]
        public int TimeoutSegundos { get; set; } = 10;

        [Range(1, 5, ErrorMessage = "Máximo de tentativas deve estar entre 1 e 5")]
        public int MaxTentativas { get; set; } = 3;

        public bool Ativo { get; set; } = true;
    }

    /// <summary>
    /// DTO para criação de log de webhook
    /// </summary>
    public class WebhookLogCreateDto
    {
        [Required]
        public string Source { get; set; } = string.Empty;

        [Required]
        public string EventType { get; set; } = string.Empty;

        [Required]
        public string Payload { get; set; } = string.Empty;

        [Required]
        public string CorrelationId { get; set; } = string.Empty;

        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    }
}