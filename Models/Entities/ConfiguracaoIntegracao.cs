using System.ComponentModel.DataAnnotations;

namespace API.Models.Entities
{
    public class ConfiguracaoIntegracao
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Nome { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Descricao { get; set; }

        [Required, MaxLength(500)]
        public string UrlApi { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Login { get; set; } = string.Empty;

        [Required]
        public string SenhaCriptografada { get; set; } = string.Empty;

        /// <summary>
        /// Endpoints organizados em JSON flexível
        /// </summary>
        public string? Endpoints { get; set; }

        [MaxLength(10)]
        public string VersaoApi { get; set; } = "v1";

        [MaxLength(200)]
        public string EndpointLogin { get; set; } = "/auth/login";

        [MaxLength(200)]
        public string EndpointPrincipal { get; set; } = "/api";

        public string? TokenAtual { get; set; }

        public DateTime? TokenExpiracao { get; set; }

        public bool Ativo { get; set; } = true;

        [Range(5, 300)]
        public int TimeoutSegundos { get; set; } = 30;

        [Range(1, 10)]
        public int MaxTentativas { get; set; } = 3;

        public string? HeadersCustomizados { get; set; }

        public bool ConfiguracaoPadrao { get; set; } = false;

        [MaxLength(50)]
        public string RetryPolicy { get; set; } = "exponential";

        [Range(10, 3600)]
        public int RetryDelayBaseSeconds { get; set; } = 60;

        public bool EnableCircuitBreaker { get; set; } = false;

        [Range(1, 100)]
        public int CircuitBreakerThreshold { get; set; } = 5;

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        public DateTime? DataAtualizacao { get; set; }

        public int? CriadoPor { get; set; }

        public int? AtualizadoPor { get; set; }

        public DateTime Version { get; set; } = DateTime.UtcNow;

        // Navegação
        public virtual ICollection<EndpointConfiguracao> EndpointsConfiguracao { get; set; } = new List<EndpointConfiguracao>();
        public virtual ICollection<LogSincronizacao> LogsSincronizacao { get; set; } = new List<LogSincronizacao>();
    }
}