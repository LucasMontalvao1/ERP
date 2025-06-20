using System.Text.Json.Serialization;

namespace API.Models.DTOs.SalesForce
{
    /// <summary>
    /// Resultado de operações do SalesForce
    /// </summary>
    public class SalesForceResult<T>
    {
        public bool IsSuccess { get; set; } 
        public T? Data { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Criar resultado de sucesso
        /// </summary>
        public static SalesForceResult<T> CreateSuccess(T data, string message = "Operação realizada com sucesso")
        {
            return new SalesForceResult<T>
            {
                IsSuccess = true, 
                Data = data,
                Message = message
            };
        }

        /// <summary>
        /// Criar resultado de erro
        /// </summary>
        public static SalesForceResult<T> CreateError(string message, List<string>? errors = null)
        {
            return new SalesForceResult<T>
            {
                IsSuccess = false, 
                Message = message,
                Errors = errors ?? new List<string>()
            };
        }

        /// <summary>
        /// Propriedade de conveniência para compatibilidade
        /// </summary>
        public bool Success => IsSuccess; 
    }

    /// <summary>
    /// DTO de resposta da integração de atividade
    /// </summary>
    public class AtividadeIntegrationResponseDto
    {
        public string? ExternalId { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
        public int ResponseTime { get; set; }
    }
}