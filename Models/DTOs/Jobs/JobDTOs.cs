using System.ComponentModel.DataAnnotations;

namespace API.Models.DTOs.Jobs
{
    /// <summary>
    /// DTO para agendar job de integração
    /// </summary>
    public class ScheduleIntegrationJobDto
    {
        [Required(ErrorMessage = "ID da atividade é obrigatório")]
        public string AtividadeId { get; set; } = string.Empty;

        /// <summary>
        /// 1=Create, 2=Update, 3=Delete
        /// </summary>
        [Range(1, 3, ErrorMessage = "Tipo de operação deve ser 1, 2 ou 3")]
        public int TipoOperacao { get; set; }

        /// <summary>
        /// 1=Muito Alta, 5=Normal, 9=Baixa
        /// </summary>
        [Range(1, 9, ErrorMessage = "Prioridade deve estar entre 1 e 9")]
        public int Prioridade { get; set; } = 5;

        public DateTime? ExecutarEm { get; set; }
    }

    /// <summary>
    /// DTO para agendar job de email
    /// </summary>
    public class ScheduleEmailJobDto
    {
        [Required(ErrorMessage = "Destinatário é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string To { get; set; } = string.Empty;

        [Required(ErrorMessage = "Assunto é obrigatório")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Corpo do email é obrigatório")]
        public string Body { get; set; } = string.Empty;

        public bool IsHtml { get; set; } = true;

        public List<string>? Attachments { get; set; }

        public DateTime? ExecutarEm { get; set; }

        public int Prioridade { get; set; } = 5;
    }

    /// <summary>
    /// DTO para agendar job de relatório
    /// </summary>
    public class ScheduleReportJobDto
    {
        [Required(ErrorMessage = "Tipo de relatório é obrigatório")]
        public string ReportType { get; set; } = string.Empty;

        public DateTime DataInicio { get; set; }

        public DateTime DataFim { get; set; }

        [Required(ErrorMessage = "Pelo menos um destinatário é obrigatório")]
        public List<string> Recipients { get; set; } = new();

        public Dictionary<string, object> Parameters { get; set; } = new();

        public DateTime? ExecutarEm { get; set; }
    }

    /// <summary>
    /// DTO para informações de job
    /// </summary>
    public class JobInfoDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Queue { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public DateTime? ScheduledAt { get; set; }
        public DateTime? ExecutedAt { get; set; }
        public string? Parameters { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public string? Reason { get; set; }
        public int AttemptCount { get; set; }
        public DateTime? NextExecution { get; set; }
    }

    /// <summary>
    /// DTO para detalhes de job
    /// </summary>
    public class JobDetailDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Queue { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public string? Reason { get; set; }
        public int AttemptCount { get; set; }
        public DateTime? NextExecution { get; set; }
        public string? Arguments { get; set; }
        public string? Exception { get; set; }
        public List<JobLogDto> History { get; set; } = new();
    }

    /// <summary>
    /// DTO para filtro de jobs
    /// </summary>
    public class JobFilterDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Status { get; set; }
        public string? Queue { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    /// <summary>
    /// DTO para estatísticas de jobs
    /// </summary>
    public class JobStatisticsDto
    {
        public int TotalJobs { get; set; }
        public int SucceededJobs { get; set; }
        public int FailedJobs { get; set; }
        public int ProcessingJobs { get; set; }
        public int ScheduledJobs { get; set; }
        public int EnqueuedJobs { get; set; }
        public double SuccessRate { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public List<JobStatisticDetail> Daily { get; set; } = new();
    }

    public class JobStatisticDetail
    {
        public DateTime Date { get; set; }
        public int Total { get; set; }
        public int Succeeded { get; set; }
        public int Failed { get; set; }
    }

    /// <summary>
    /// DTO para jobs recorrentes
    /// </summary>
    public class RecurringJobDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Cron { get; set; } = string.Empty;
        public string Queue { get; set; } = string.Empty;
        public DateTime? LastExecution { get; set; }
        public DateTime? NextExecution { get; set; }
        public bool IsActive { get; set; }
        public string? LastJobId { get; set; }
        public string? LastJobState { get; set; }
    }

    /// <summary>
    /// DTO para logs de job
    /// </summary>
    public class JobLogDto
    {
        public DateTime Timestamp { get; set; }
        public string State { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public string? Data { get; set; }
    }
}