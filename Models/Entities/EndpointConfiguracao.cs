using System.ComponentModel.DataAnnotations;

namespace API.Models.Entities
{
    public class EndpointConfiguracao
    {
        public int Id { get; set; }

        public int ConfiguracaoId { get; set; }

        [Required, MaxLength(50)]
        public string Categoria { get; set; } = string.Empty; 

        [Required, MaxLength(50)]
        public string Acao { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string Endpoint { get; set; } = string.Empty;

        [MaxLength(10)]
        public string MetodoHttp { get; set; } = "POST";

        public string? HeadersEspecificos { get; set; }

        public int? TimeoutEspecifico { get; set; }

        public bool Ativo { get; set; } = true;

        public int OrdemPrioridade { get; set; } = 0;

        public string? Observacoes { get; set; }

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        public DateTime? DataAtualizacao { get; set; }

        // Navegação
        public virtual ConfiguracaoIntegracao ConfiguracaoIntegracao { get; set; } = null!;
    }
}