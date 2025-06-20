using System.ComponentModel.DataAnnotations;

namespace API.Models.Entities
{
    public class Atividade
    {
        [Key]
        public string CodAtiv { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Ramo { get; set; } = string.Empty;

        [Range(-99.99, 99.99)]
        public decimal PercDesc { get; set; }

        [MaxLength(1)]
        public string CalculaSt { get; set; } = "N";

        /// <summary>
        /// 0=Pendente, 1=Sincronizado, 2=Erro, 3=Reprocessando, 4=Cancelado
        /// </summary>
        public int StatusSincronizacao { get; set; } = 0;

        public DateTime? DataUltimaSincronizacao { get; set; }

        public int TentativasSincronizacao { get; set; } = 0;

        public string? UltimoErroSincronizacao { get; set; }

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        public DateTime? DataAtualizacao { get; set; }

        public int? CriadoPor { get; set; }

        public int? AtualizadoPor { get; set; }

        public DateTime Version { get; set; } = DateTime.UtcNow;

        // Navegação
        public virtual Usuario? UsuarioCriador { get; set; }
        public virtual Usuario? UsuarioAtualizador { get; set; }
        public virtual ICollection<LogSincronizacao> LogsSincronizacao { get; set; } = new List<LogSincronizacao>();
    }
}