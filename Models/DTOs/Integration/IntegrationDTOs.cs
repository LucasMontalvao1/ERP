using System.ComponentModel.DataAnnotations;

namespace API.Models.DTOs.Integration
{
    /// <summary>
    /// DTO para resultado de integração
    /// </summary>
    public class IntegrationResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ExternalId { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
        public int ResponseTime { get; set; }
        public object? Data { get; set; }
    }

    /// <summary>
    /// DTO para configuração de integração
    /// </summary>
    public class IntegrationConfigDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public string UrlApi { get; set; } = string.Empty;
        public string VersaoApi { get; set; } = string.Empty;
        public bool Ativo { get; set; }
        public bool ConfiguracaoPadrao { get; set; }
        public int TimeoutSegundos { get; set; }
        public int MaxTentativas { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime? UltimaConexao { get; set; }
    }

    /// <summary>
    /// DTO para criar configuração de integração
    /// </summary>
    public class CreateIntegrationConfigDto
    {
        [Required(ErrorMessage = "Nome é obrigatório")]
        [StringLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Descrição deve ter no máximo 255 caracteres")]
        public string? Descricao { get; set; }

        [Required(ErrorMessage = "URL da API é obrigatória")]
        [Url(ErrorMessage = "URL inválida")]
        [StringLength(500, ErrorMessage = "URL deve ter no máximo 500 caracteres")]
        public string UrlApi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Login é obrigatório")]
        [StringLength(100, ErrorMessage = "Login deve ter no máximo 100 caracteres")]
        public string Login { get; set; } = string.Empty;

        [Required(ErrorMessage = "Senha é obrigatória")]
        public string Senha { get; set; } = string.Empty;

        [StringLength(10, ErrorMessage = "Versão da API deve ter no máximo 10 caracteres")]
        public string VersaoApi { get; set; } = "v1";

        [Range(5, 300, ErrorMessage = "Timeout deve estar entre 5 e 300 segundos")]
        public int TimeoutSegundos { get; set; } = 30;

        [Range(1, 10, ErrorMessage = "Máximo de tentativas deve estar entre 1 e 10")]
        public int MaxTentativas { get; set; } = 3;

        public bool Ativo { get; set; } = true;
        public bool ConfiguracaoPadrao { get; set; } = false;
    }

    /// <summary>
    /// DTO para atualizar configuração de integração
    /// </summary>
    public class UpdateIntegrationConfigDto
    {
        [Required(ErrorMessage = "Nome é obrigatório")]
        [StringLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Descrição deve ter no máximo 255 caracteres")]
        public string? Descricao { get; set; }

        [Required(ErrorMessage = "URL da API é obrigatória")]
        [Url(ErrorMessage = "URL inválida")]
        [StringLength(500, ErrorMessage = "URL deve ter no máximo 500 caracteres")]
        public string UrlApi { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Login deve ter no máximo 100 caracteres")]
        public string? Login { get; set; }

        public string? Senha { get; set; }

        [StringLength(10, ErrorMessage = "Versão da API deve ter no máximo 10 caracteres")]
        public string VersaoApi { get; set; } = "v1";

        [Range(5, 300, ErrorMessage = "Timeout deve estar entre 5 e 300 segundos")]
        public int TimeoutSegundos { get; set; } = 30;

        [Range(1, 10, ErrorMessage = "Máximo de tentativas deve estar entre 1 e 10")]
        public int MaxTentativas { get; set; } = 3;

        public bool Ativo { get; set; } = true;
        public bool ConfiguracaoPadrao { get; set; } = false;
    }

    /// <summary>
    /// DTO para log de sincronização
    /// </summary>
    public class SyncLogDto
    {
        public int Id { get; set; }
        public string CodAtiv { get; set; } = string.Empty;
        public string TipoOperacao { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? EndpointUsado { get; set; }
        public string? MetodoHttp { get; set; }
        public int? CodigoHttp { get; set; }
        public string? MensagemErro { get; set; }
        public long TempoProcessamento { get; set; }
        public int NumeroTentativa { get; set; }
        public DateTime? ProximaTentativa { get; set; }
        public string? CorrelationId { get; set; }
        public DateTime DataCriacao { get; set; }
    }

    /// <summary>
    /// DTO para filtro de logs de sincronização
    /// </summary>
    public class SyncLogFilterDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? CodAtiv { get; set; }
        public int? TipoOperacao { get; set; }
        public int? StatusProcessamento { get; set; }
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public string? CorrelationId { get; set; }
    }

    /// <summary>
    /// DTO para estatísticas de integração
    /// </summary>
    public class IntegrationStatisticsDto
    {
        public int TotalIntegracoes { get; set; }
        public int IntegracoesSucesso { get; set; }
        public int IntegracoesErro { get; set; }
        public int IntegracoesPendentes { get; set; }
        public double TaxaSucesso { get; set; }
        public long TempoMedioProcessamento { get; set; }
        public DateTime PeriodoInicio { get; set; }
        public DateTime PeriodoFim { get; set; }
        public List<IntegrationStatisticDaily> Diario { get; set; } = new();
        public List<IntegrationStatisticByOperation> PorOperacao { get; set; } = new();
    }

    /// <summary>
    /// DTO para estatísticas diárias de integração
    /// </summary>
    public class IntegrationStatisticDaily
    {
        public DateTime Data { get; set; }
        public int Total { get; set; }
        public int Sucesso { get; set; }
        public int Erro { get; set; }
        public int Pendente { get; set; }
    }

    /// <summary>
    /// DTO para estatísticas por operação
    /// </summary>
    public class IntegrationStatisticByOperation
    {
        public string TipoOperacao { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Sucesso { get; set; }
        public int Erro { get; set; }
        public double TaxaSucesso { get; set; }
    }

    /// <summary>
    /// DTO para processamento em lote
    /// </summary>
    public class BatchProcessResultDto
    {
        public int TotalProcessados { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> Sucessos { get; set; } = new();
        public List<string> Falhas { get; set; } = new();
        public List<string> ErrosDetalhados { get; set; } = new();
        public string CorrelationId { get; set; } = string.Empty;
        public DateTime ProcessadoEm { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// DTO para dados de teste de integração completa
    /// </summary>
    public class CompleteIntegrationTestDto
    {
        public bool AuthenticationTest { get; set; }
        public bool DataSendTest { get; set; }
        public bool WebhookTest { get; set; }
        public int TotalResponseTime { get; set; }
        public List<string> TestSteps { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public DateTime TestedAt { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para retry de integração
    /// </summary>
    public class RetryIntegrationDto
    {
        public string CodAtiv { get; set; } = string.Empty;
        public int LogId { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public bool ForcarTentativa { get; set; } = false;
    }

    /// <summary>
    /// DTO para configuração de endpoints
    /// </summary>
    public class EndpointConfigDto
    {
        public int Id { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public string Acao { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string MetodoHttp { get; set; } = string.Empty;
        public bool Ativo { get; set; }
        public int OrdemPrioridade { get; set; }
        public Dictionary<string, string>? HeadersEspecificos { get; set; }
        public int? TimeoutEspecifico { get; set; }
        public string? Observacoes { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime? DataAtualizacao { get; set; }
    }

    /// <summary>
    /// DTO para criar configuração de endpoint
    /// </summary>
    public class CreateEndpointConfigDto
    {
        [Required(ErrorMessage = "Categoria é obrigatória")]
        [StringLength(50, ErrorMessage = "Categoria deve ter no máximo 50 caracteres")]
        public string Categoria { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ação é obrigatória")]
        [StringLength(50, ErrorMessage = "Ação deve ter no máximo 50 caracteres")]
        public string Acao { get; set; } = string.Empty;

        [Required(ErrorMessage = "Endpoint é obrigatório")]
        [StringLength(500, ErrorMessage = "Endpoint deve ter no máximo 500 caracteres")]
        public string Endpoint { get; set; } = string.Empty;

        [Required(ErrorMessage = "Método HTTP é obrigatório")]
        [StringLength(10, ErrorMessage = "Método HTTP deve ter no máximo 10 caracteres")]
        public string MetodoHttp { get; set; } = "POST";

        public Dictionary<string, string>? HeadersEspecificos { get; set; }

        [Range(5, 300, ErrorMessage = "Timeout específico deve estar entre 5 e 300 segundos")]
        public int? TimeoutEspecifico { get; set; }

        public bool Ativo { get; set; } = true;

        [Range(0, 100, ErrorMessage = "Ordem de prioridade deve estar entre 0 e 100")]
        public int OrdemPrioridade { get; set; } = 0;

        [StringLength(500, ErrorMessage = "Observações devem ter no máximo 500 caracteres")]
        public string? Observacoes { get; set; }
    }

    /// <summary>
    /// DTO para atualizar configuração de endpoint
    /// </summary>
    public class UpdateEndpointConfigDto
    {
        [Required(ErrorMessage = "Categoria é obrigatória")]
        [StringLength(50, ErrorMessage = "Categoria deve ter no máximo 50 caracteres")]
        public string Categoria { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ação é obrigatória")]
        [StringLength(50, ErrorMessage = "Ação deve ter no máximo 50 caracteres")]
        public string Acao { get; set; } = string.Empty;

        [Required(ErrorMessage = "Endpoint é obrigatório")]
        [StringLength(500, ErrorMessage = "Endpoint deve ter no máximo 500 caracteres")]
        public string Endpoint { get; set; } = string.Empty;

        [Required(ErrorMessage = "Método HTTP é obrigatório")]
        [StringLength(10, ErrorMessage = "Método HTTP deve ter no máximo 10 caracteres")]
        public string MetodoHttp { get; set; } = "POST";

        public Dictionary<string, string>? HeadersEspecificos { get; set; }

        [Range(5, 300, ErrorMessage = "Timeout específico deve estar entre 5 e 300 segundos")]
        public int? TimeoutEspecifico { get; set; }

        public bool Ativo { get; set; } = true;

        [Range(0, 100, ErrorMessage = "Ordem de prioridade deve estar entre 0 e 100")]
        public int OrdemPrioridade { get; set; } = 0;

        [StringLength(500, ErrorMessage = "Observações devem ter no máximo 500 caracteres")]
        public string? Observacoes { get; set; }
    }

    /// <summary>
    /// DTO para monitoramento de integrações
    /// </summary>
    public class IntegrationMonitorDto
    {
        public string Status { get; set; } = string.Empty;
        public int FilaPendente { get; set; }
        public int FilaProcessando { get; set; }
        public int UltimasHoras24Sucesso { get; set; }
        public int UltimasHoras24Erro { get; set; }
        public double TaxaSucessoUltimas24h { get; set; }
        public DateTime UltimaIntegracao { get; set; }
        public List<string> AlertasAtivos { get; set; } = new();
        public Dictionary<string, object> Metricas { get; set; } = new();
    }

    /// <summary>
    /// DTO para webhook de integração
    /// </summary>
    public class WebhookDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Evento { get; set; } = string.Empty;
        public bool Ativo { get; set; }
        public string? Secret { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
        public int MaxTentativas { get; set; } = 3;
        public int TimeoutSegundos { get; set; } = 30;
        public DateTime DataCriacao { get; set; }
        public DateTime? UltimoDisparo { get; set; }
    }

    /// <summary>
    /// DTO para criar webhook
    /// </summary>
    public class CreateWebhookDto
    {
        [Required(ErrorMessage = "Nome é obrigatório")]
        [StringLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "URL é obrigatória")]
        [Url(ErrorMessage = "URL inválida")]
        [StringLength(500, ErrorMessage = "URL deve ter no máximo 500 caracteres")]
        public string Url { get; set; } = string.Empty;

        [Required(ErrorMessage = "Evento é obrigatório")]
        [StringLength(50, ErrorMessage = "Evento deve ter no máximo 50 caracteres")]
        public string Evento { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Secret deve ter no máximo 100 caracteres")]
        public string? Secret { get; set; }

        public Dictionary<string, string>? Headers { get; set; }

        [Range(1, 10, ErrorMessage = "Máximo de tentativas deve estar entre 1 e 10")]
        public int MaxTentativas { get; set; } = 3;

        [Range(5, 300, ErrorMessage = "Timeout deve estar entre 5 e 300 segundos")]
        public int TimeoutSegundos { get; set; } = 30;

        public bool Ativo { get; set; } = true;
    }

    /// <summary>
    /// DTO para resultado de webhook
    /// </summary>
    public class WebhookResultDto
    {
        public bool Success { get; set; }
        public int HttpStatusCode { get; set; }
        public string ResponseBody { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public int ResponseTime { get; set; }
        public DateTime DispatchedAt { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para fila de processamento
    /// </summary>
    public class QueueItemDto
    {
        public int Id { get; set; }
        public string CodAtiv { get; set; } = string.Empty;
        public string TipoOperacao { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Prioridade { get; set; }
        public string? DadosPayload { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime? DataProcessamento { get; set; }
        public int NumeroTentativas { get; set; }
        public DateTime? ProximaTentativa { get; set; }
        public string? CorrelationId { get; set; }
    }

    /// <summary>
    /// DTO para criar item na fila
    /// </summary>
    public class CreateQueueItemDto
    {
        [Required(ErrorMessage = "Código da atividade é obrigatório")]
        public string CodAtiv { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tipo de operação é obrigatório")]
        public string TipoOperacao { get; set; } = string.Empty;

        [Range(0, 10, ErrorMessage = "Prioridade deve estar entre 0 e 10")]
        public int Prioridade { get; set; } = 0;

        public object? DadosPayload { get; set; }

        public DateTime? AgendarPara { get; set; }

        public string? CorrelationId { get; set; }
    }
}