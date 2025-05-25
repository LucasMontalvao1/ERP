using System.Net;
using System.Text.Json;

namespace API.Middlewares
{
    /// <summary>
    /// Middleware para tratamento centralizado de exceções
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            _logger.LogError(exception, "Ocorreu uma exceção não tratada: {Message}", exception.Message);

            var response = context.Response;
            response.ContentType = "application/json";

            // Definir código de status e mensagem de erro com base no tipo de exceção
            var (statusCode, message) = GetStatusCodeAndMessage(exception);
            response.StatusCode = (int)statusCode;

            // Criar resposta de erro
            var errorResponse = new
            {
                Status = (int)statusCode,
                Message = message,
                // Em desenvolvimento, inclua detalhes da exceção
                // Em produção, omita detalhes para evitar vazamento de informações
                Detail = context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()
                    ? exception.ToString()
                    : null
            };

            var result = JsonSerializer.Serialize(errorResponse);
            await response.WriteAsync(result);
        }

        private (HttpStatusCode statusCode, string message) GetStatusCodeAndMessage(Exception exception)
        {
            // Aqui você pode mapear tipos específicos de exceção para códigos de status HTTP
            return exception switch
            {
                // Por exemplo, para validações com FluentValidation
                FluentValidation.ValidationException validationException =>
                    (HttpStatusCode.BadRequest, "Erro de validação: " + string.Join(", ",
                        validationException.Errors.Select(e => e.ErrorMessage))),

                // Exceção de entidade não encontrada
                KeyNotFoundException => (HttpStatusCode.NotFound, "Recurso não encontrado"),

                // Exceção de acesso não autorizado
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Acesso não autorizado"),

                // Exceção padrão
                _ => (HttpStatusCode.InternalServerError, "Ocorreu um erro interno no servidor")
            };
        }
    }
}
