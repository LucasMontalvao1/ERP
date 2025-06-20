using System.ComponentModel.DataAnnotations;

namespace API.Models.Entities
{
    public class LogSincronizacao
    {
        public int Id { get; set; }

        public int ConfiguracaoId { get; set; }

        [Required, MaxLength(50)]
        public string CodAtiv { get; set; } = string.Empty;

        /// <summary>
        /// 1=Create, 2=Update, 3=Delete
        /// </summary>
        public int TipoOperacao { get; set; }

        /// <summary>
        /// 0=Iniciado, 1=Sucesso, 2=Erro, 3=Timeout, 4=Cancelado, 5=Reprocessando
        /// </summary>
        public int StatusProcessamento { get; set; }

        [MaxLength(50)]
        public string? CategoriaEndpoint { get; set; }

        [MaxLength(50)]
        public string? AcaoEndpoint { get; set; }

        [MaxLength(500)]
        public string? EndpointUsado { get; set; }

        [MaxLength(10)]
        public string? MetodoHttpUsado { get; set; }

        public string? PayloadEnviado { get; set; }

        public string? RespostaRecebida { get; set; }

        public int? CodigoHttp { get; set; }

        public string? MensagemErro { get; set; }

        public long TempoProcessamentoMs { get; set; } = 0;

        public int NumeroTentativa { get; set; } = 1;

        public DateTime? ProximaTentativa { get; set; }

        [MaxLength(100)]
        public string? JobId { get; set; }

        public string? Metadados { get; set; }

        [MaxLength(100)]
        public string? CorrelationId { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        [MaxLength(45)]
        public string? IpOrigem { get; set; }

        public int? TamanhoPayloadBytes { get; set; }

        public int? TamanhoRespostaBytes { get; set; }

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        public DateTime? DataAtualizacao { get; set; }

        public DateTime Version { get; set; } = DateTime.UtcNow;

        // Navegação
        public virtual ConfiguracaoIntegracao ConfiguracaoIntegracao { get; set; } = null!;
        public virtual Atividade Atividade { get; set; } = null!;
    }
}