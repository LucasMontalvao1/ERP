using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;

namespace API.Filters
{
    public class ApiExceptionFilter : ExceptionFilterAttribute
    {
        private readonly ILogger<ApiExceptionFilter> _logger;

        public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger)
        {
            _logger = logger;
        }

        public override void OnException(ExceptionContext context)
        {
            var exception = context.Exception;

            _logger.LogError(exception, "Exceção capturada pelo filtro global: {Message}", exception.Message);

            // Respostas específicas para diferentes tipos de exceção
            switch (exception)
            {
                case ValidationException ex:
                    HandleValidationException(context, ex);
                    return;

                case UnauthorizedAccessException ex:
                    HandleUnauthorizedException(context, ex);
                    return;

                case InvalidOperationException ex:
                    HandleInvalidOperationException(context, ex);
                    return;

                case ArgumentException ex:
                    HandleArgumentException(context, ex);
                    return;

                case KeyNotFoundException ex:
                    HandleNotFoundException(context, ex);
                    return;

                default:
                    HandleUnknownException(context, exception);
                    return;
            }
        }

        private void HandleValidationException(ExceptionContext context, ValidationException ex)
        {
            var problemDetails = new ValidationProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Erro de validação",
                Detail = "Um ou mais erros de validação ocorreram"
            };

            foreach (var error in ex.Errors)
            {
                problemDetails.Errors.Add(
                    error.PropertyName,
                    new[] { error.ErrorMessage }
                );
            }

            context.Result = new BadRequestObjectResult(problemDetails);
            context.ExceptionHandled = true;
        }

        private void HandleUnauthorizedException(ExceptionContext context, UnauthorizedAccessException ex)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Não autorizado",
                Detail = ex.Message ?? "Usuário não autenticado ou sem permissão"
            };

            context.Result = new UnauthorizedObjectResult(problemDetails);
            context.ExceptionHandled = true;
        }

        private void HandleInvalidOperationException(ExceptionContext context, InvalidOperationException ex)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Operação inválida",
                Detail = ex.Message
            };

            context.Result = new BadRequestObjectResult(problemDetails);
            context.ExceptionHandled = true;
        }

        private void HandleArgumentException(ExceptionContext context, ArgumentException ex)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Argumento inválido",
                Detail = ex.Message
            };

            context.Result = new BadRequestObjectResult(problemDetails);
            context.ExceptionHandled = true;
        }

        private void HandleNotFoundException(ExceptionContext context, KeyNotFoundException ex)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Recurso não encontrado",
                Detail = ex.Message ?? "O recurso solicitado não foi encontrado"
            };

            context.Result = new NotFoundObjectResult(problemDetails);
            context.ExceptionHandled = true;
        }

        private void HandleUnknownException(ExceptionContext context, Exception ex)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Erro interno do servidor",
                Detail = "Ocorreu um erro ao processar sua solicitação."
            };

            context.Result = new ObjectResult(problemDetails)
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
            context.ExceptionHandled = true;
        }
    }
}
