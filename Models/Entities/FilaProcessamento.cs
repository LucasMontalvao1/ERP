using System.ComponentModel.DataAnnotations;

namespace API.Models.Entities
{
    public class FilaProcessamento
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string NomeFila { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string CodAtiv { get; set; } = string.Empty;

        public int TipoOperacao { get; set; }

        /// <summary>
        /// 0=Pendente, 1=Processando, 2=Processado, 3=Erro, 4=Cancelado
        /// </summary>
        public int StatusFila { get; set; } = 0;

        [Required]
        public string MensagemJson { get; set; } = string.Empty;

        public int TentativasProcessamento { get; set; } = 0;

        public int MaxTentativas { get; set; } = 3;

        public DateTime? ProximoProcessamento { get; set; }

        [Required, MaxLength(100)]
        public string CorrelationId { get; set; } = string.Empty;

        public string? ErroProcessamento { get; set; }

        /// <summary>
        /// 1=Muito Alta, 5=Normal, 9=Baixa
        /// </summary>
        public int Prioridade { get; set; } = 5;

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        public DateTime? DataProcessamento { get; set; }

        // Navegação
        public virtual Atividade Atividade { get; set; } = null!;
    }
}