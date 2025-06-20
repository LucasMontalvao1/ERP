namespace API.Models.Responses
{
    /// <summary>
    /// Classe para resultados de serviços (não conflita com existentes)
    /// </summary>
    /// <typeparam name="T">Tipo de dados do resultado</typeparam>
    public class SalesForceServiceResult<T>
    {
        /// <summary>
        /// Indica se a operação foi bem-sucedida
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Mensagem descritiva do resultado
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Dados retornados pela operação
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// Lista de erros encontrados
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Timestamp da operação
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Construtor protegido para permitir herança
        /// </summary>
        protected SalesForceServiceResult()
        {
        }

        /// <summary>
        /// Criar resultado de sucesso
        /// </summary>
        /// <param name="data">Dados do resultado</param>
        /// <param name="message">Mensagem de sucesso</param>
        /// <returns>SalesForceServiceResult de sucesso</returns>
        public static SalesForceServiceResult<T> CreateSuccess(T data, string message = "Operação realizada com sucesso")
        {
            return new SalesForceServiceResult<T>
            {
                Success = true,
                Message = message,
                Data = data,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Criar resultado de falha
        /// </summary>
        /// <param name="message">Mensagem de erro</param>
        /// <param name="errors">Lista de erros</param>
        /// <returns>SalesForceServiceResult de falha</returns>
        public static SalesForceServiceResult<T> CreateFailure(string message, List<string>? errors = null)
        {
            return new SalesForceServiceResult<T>
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string>(),
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Criar resultado de falha com erro único
        /// </summary>
        /// <param name="message">Mensagem de erro</param>
        /// <param name="error">Erro específico</param>
        /// <returns>SalesForceServiceResult de falha</returns>
        public static SalesForceServiceResult<T> CreateFailure(string message, string error)
        {
            return new SalesForceServiceResult<T>
            {
                Success = false,
                Message = message,
                Errors = new List<string> { error },
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// SalesForceServiceResult sem dados genéricos
    /// </summary>
    public class SalesForceServiceResult : SalesForceServiceResult<object>
    {
        /// <summary>
        /// Construtor público para a classe não genérica
        /// </summary>
        public SalesForceServiceResult() : base()
        {
        }

        /// <summary>
        /// Criar resultado de sucesso sem dados
        /// </summary>
        /// <param name="message">Mensagem de sucesso</param>
        /// <returns>SalesForceServiceResult de sucesso</returns>
        public static new SalesForceServiceResult CreateSuccess(string message = "Operação realizada com sucesso")
        {
            return new SalesForceServiceResult
            {
                Success = true,
                Message = message,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Criar resultado de falha sem dados
        /// </summary>
        /// <param name="message">Mensagem de erro</param>
        /// <param name="errors">Lista de erros</param>
        /// <returns>SalesForceServiceResult de falha</returns>
        public static new SalesForceServiceResult CreateFailure(string message, List<string>? errors = null)
        {
            return new SalesForceServiceResult
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string>(),
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Criar resultado de falha com erro único
        /// </summary>
        /// <param name="message">Mensagem de erro</param>
        /// <param name="error">Erro específico</param>
        /// <returns>SalesForceServiceResult de falha</returns>
        public static new SalesForceServiceResult CreateFailure(string message, string error)
        {
            return new SalesForceServiceResult
            {
                Success = false,
                Message = message,
                Errors = new List<string> { error },
                Timestamp = DateTime.UtcNow
            };
        }
    }
}