using System.ComponentModel.DataAnnotations;
using API.Constants;

namespace API.Models.DTOs.Atividade
{
    /// <summary>
    /// DTO para criação de atividade
    /// </summary>
    public class CreateAtividadeDto
    {
        [Required(ErrorMessage = "Código da atividade é obrigatório")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Código deve ter entre 1 e 50 caracteres")]
        public string CodAtiv { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ramo é obrigatório")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Ramo deve ter entre 1 e 100 caracteres")]
        public string Ramo { get; set; } = string.Empty;

        [Range(-99.99, 99.99, ErrorMessage = "Percentual de desconto deve estar entre -99.99 e 99.99")]
        public decimal PercDesc { get; set; }

        [StringLength(1, ErrorMessage = "CalculaSt deve ter apenas 1 caractere")]
        [RegularExpression("^[SN]$", ErrorMessage = "CalculaSt deve ser 'S' ou 'N'")]
        public string CalculaSt { get; set; } = "N";

        /// <summary>
        /// Se true, força sincronização imediata com API externa
        /// </summary>
        public bool ForcaSincronizacao { get; set; } = false;
    }

    /// <summary>
    /// DTO para atualização de atividade
    /// </summary>
    public class UpdateAtividadeDto
    {
        [Required(ErrorMessage = "Ramo é obrigatório")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Ramo deve ter entre 1 e 100 caracteres")]
        public string Ramo { get; set; } = string.Empty;

        [Range(-99.99, 99.99, ErrorMessage = "Percentual de desconto deve estar entre -99.99 e 99.99")]
        public decimal PercDesc { get; set; }

        [StringLength(1, ErrorMessage = "CalculaSt deve ter apenas 1 caractere")]
        [RegularExpression("^[SN]$", ErrorMessage = "CalculaSt deve ser 'S' ou 'N'")]
        public string CalculaSt { get; set; } = "N";

        /// <summary>
        /// Se true, força sincronização imediata com API externa
        /// </summary>
        public bool ForcaSincronizacao { get; set; } = false;
    }

    /// <summary>
    /// DTO para resposta de atividade
    /// </summary>
    public class AtividadeResponseDto
    {
        public string CodAtiv { get; set; } = string.Empty;
        public string Ramo { get; set; } = string.Empty;
        public decimal PercDesc { get; set; }
        public string CalculaSt { get; set; } = string.Empty;
        public int StatusSincronizacao { get; set; }
        public string StatusSincronizacaoDescricao { get; set; } = string.Empty;
        public DateTime? DataUltimaSincronizacao { get; set; }
        public int TentativasSincronizacao { get; set; }
        public string? UltimoErroSincronizacao { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime? DataAtualizacao { get; set; }
        public string? NomeCriador { get; set; }
        public string? NomeAtualizador { get; set; }
        /// <summary>
        /// Informações de integração com sistema externo
        /// </summary>
        public AtividadeIntegrationInfoDto? IntegrationInfo { get; set; }
    }

    /// <summary>
    /// DTO para listagem paginada de atividades
    /// </summary>
    public class AtividadeListDto
    {
        public string CodAtiv { get; set; } = string.Empty;
        public string Ramo { get; set; } = string.Empty;
        public decimal PercDesc { get; set; }
        public string CalculaSt { get; set; } = string.Empty;
        public int StatusSincronizacao { get; set; }
        public string StatusSincronizacaoDescricao { get; set; } = string.Empty;
        public DateTime? DataUltimaSincronizacao { get; set; }
        public DateTime DataCriacao { get; set; }
    }

    /// <summary>
    /// Filtros para busca de atividades
    /// </summary>
    public class AtividadeFilterDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = ApiConstants.Defaults.PageSize;
        public string? CodAtiv { get; set; }
        public string? Ramo { get; set; }
        public string? CalculaSt { get; set; }
        public int? StatusSincronizacao { get; set; }
        public DateTime? DataCriacaoInicio { get; set; }
        public DateTime? DataCriacaoFim { get; set; }
        public string? OrderBy { get; set; } = "DataCriacao";
        public string? OrderDirection { get; set; } = "DESC";
    }

    /// <summary>
    /// DTO para sincronização com API externa
    /// </summary>
    public class AtividadeSyncDto
    {
        public string CodAtiv { get; set; } = string.Empty;
        public string Ramo { get; set; } = string.Empty;
        public decimal PercDesc { get; set; }
        public string CalculaSt { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; }
        public DateTime? DataAtualizacao { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para resposta de sincronização em lote
    /// </summary>
    public class BatchSyncResponseDto
    {
        public int TotalProcessados { get; set; }
        public int Sucessos { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public int Falhas { get; set; }
        public List<string> ErrosDetalhados { get; set; } = new();
        public string CorrelationId { get; set; } = string.Empty;
        public DateTime ProcessadoEm { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// DTO com informações de integração da atividade
    /// </summary>
    public class AtividadeIntegrationInfoDto
    {
        /// <summary>
        /// Status da integração
        /// </summary>
        public string IntegrationStatus { get; set; } = string.Empty;

        /// <summary>
        /// ID externo retornado pela API
        /// </summary>
        public string? ExternalId { get; set; }

        /// <summary>
        /// Data/hora da última sincronização
        /// </summary>
        public DateTime? LastSyncAt { get; set; }

        /// <summary>
        /// Mensagem da sincronização
        /// </summary>
        public string? SyncMessage { get; set; }

        /// <summary>
        /// ID de correlação usado na integração
        /// </summary>
        public string CorrelationId { get; set; } = string.Empty;
    }
}