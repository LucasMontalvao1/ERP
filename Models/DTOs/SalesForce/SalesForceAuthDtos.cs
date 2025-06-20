using System.Text.Json.Serialization;

namespace API.Models.DTOs.SalesForce
{
    /// <summary>
    /// DTO para dados de autenticação do SalesForce (usado internamente)
    /// </summary>
    public class SalesForceAuthDto
    {
        /// <summary>
        /// Token de acesso para autenticação
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// URL da instância do SalesForce
        /// </summary>
        public string InstanceUrl { get; set; } = string.Empty;

        /// <summary>
        /// Data e hora de expiração do token
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Tipo do token (geralmente "Bearer")
        /// </summary>
        public string TokenType { get; set; } = "Bearer";

        /// <summary>
        /// Dados extras retornados pela API
        /// </summary>
        public Dictionary<string, object> ExtraData { get; set; } = new();
    }

    /// <summary>
    /// DTO para resposta customizada da sua API SalesForce
    /// </summary>
    public class CustomSalesForceAuthResponse
    {
        /// <summary>
        /// Indica se a autenticação foi bem-sucedida
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Data de criação do token
        /// </summary>
        [JsonPropertyName("data_Criacao")]
        public string? Data_Criacao { get; set; }

        /// <summary>
        /// Data de expiração do token
        /// </summary>
        [JsonPropertyName("data_Expiracao")]
        public string? Data_Expiracao { get; set; }

        /// <summary>
        /// Token de acesso retornado pela API
        /// </summary>
        [JsonPropertyName("token_De_Acesso")]
        public string? Token_De_Acesso { get; set; }

        /// <summary>
        /// Mensagem de resposta da API
        /// </summary>
        [JsonPropertyName("resposta")]
        public string? Resposta { get; set; }
    }

    /// <summary>
    /// DTO para request de login na API customizada
    /// </summary>
    public class CustomSalesForceLoginRequest
    {
        /// <summary>
        /// Login do usuário (criptografado)
        /// </summary>
        [JsonPropertyName("login")]
        public string Login { get; set; } = string.Empty;

        /// <summary>
        /// Senha do usuário (criptografada)
        /// </summary>
        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para erro de autenticação
    /// </summary>
    public class SalesForceAuthErrorResponse
    {
        /// <summary>
        /// Indica se houve sucesso (sempre false para erros)
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Código do erro
        /// </summary>
        [JsonPropertyName("error")]
        public string? Error { get; set; }

        /// <summary>
        /// Descrição detalhada do erro
        /// </summary>
        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; set; }

        /// <summary>
        /// Mensagem de erro
        /// </summary>
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        /// <summary>
        /// Timestamp do erro
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}