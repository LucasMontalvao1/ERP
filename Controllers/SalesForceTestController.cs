using API.Constants;
using API.Models.DTOs.SalesForce;
using API.Models.Responses;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class SalesForceTestController : ControllerBase
    {
        private readonly ISalesForceService _salesForceService;
        private readonly IIntegrationService _integrationService;
        private readonly ILogger<SalesForceTestController> _logger;

        public SalesForceTestController(
            ISalesForceService salesForceService,
            IIntegrationService integrationService,
            ILogger<SalesForceTestController> logger)
        {
            _salesForceService = salesForceService;
            _integrationService = integrationService;
            _logger = logger;
        }

        /// <summary>
        /// Testar login na API do SalesForce
        /// </summary>
        /// <returns>Resultado do teste de autenticação</returns>
        [HttpPost("test-login")]
        [EnableRateLimiting(ApiConstants.RateLimitPolicies.Default)]
        [ProducesResponseType(typeof(ApiResponse<SalesForceAuthTestDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> TestLogin()
        {
            var correlationId = Guid.NewGuid().ToString();

            Console.WriteLine($"=== INÍCIO TESTE SALESFORCE ===");
            Console.WriteLine($"CorrelationId: {correlationId}");
            Console.WriteLine($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC");

            _logger.LogInformation("Iniciando teste de login SalesForce. CorrelationId: {CorrelationId}", correlationId);

            var startTime = DateTime.UtcNow;
            Console.WriteLine($"Start Time: {startTime:yyyy-MM-dd HH:mm:ss.fff} UTC");

            try
            {
                Console.WriteLine("=== CHAMANDO AUTHENTICATE ASYNC ===");
                Console.WriteLine($"Antes da chamada AuthenticateAsync - {DateTime.UtcNow:HH:mm:ss.fff}");

                _logger.LogInformation("Chamando _salesForceService.AuthenticateAsync()...");

                var authResult = await _salesForceService.AuthenticateAsync();

                Console.WriteLine("=== RESULTADO DA AUTENTICAÇÃO ===");
                Console.WriteLine($"Após AuthenticateAsync - {DateTime.UtcNow:HH:mm:ss.fff}");
                Console.WriteLine($"Auth Success: {authResult.Success}");
                Console.WriteLine($"Auth Message: {authResult.Message ?? "null"}");
                Console.WriteLine($"Auth Data is null: {authResult.Data == null}");

                if (authResult.Data != null)
                {
                    Console.WriteLine($"Token Expires At: {authResult.Data.ExpiresAt}");
                    Console.WriteLine($"Instance URL: {authResult.Data.InstanceUrl}");
                    Console.WriteLine($"Token Type: {authResult.Data.TokenType}");
                    Console.WriteLine($"Access Token Length: {authResult.Data.AccessToken?.Length ?? 0}");
                    Console.WriteLine($"Extra Data Count: {authResult.Data.ExtraData?.Count ?? 0}");
                }

                var endTime = DateTime.UtcNow;
                var responseTime = (int)(endTime - startTime).TotalMilliseconds;

                Console.WriteLine($"=== TEMPO DE RESPOSTA ===");
                Console.WriteLine($"End Time: {endTime:yyyy-MM-dd HH:mm:ss.fff} UTC");
                Console.WriteLine($"Response Time: {responseTime}ms");

                if (authResult.Success)
                {
                    Console.WriteLine("=== AUTENTICAÇÃO BEM-SUCEDIDA ===");

                    // Buscar URL base
                    Console.WriteLine("Buscando Base URL...");
                    string? baseUrl = null;
                    try
                    {
                        baseUrl = await _salesForceService.GetBaseUrlAsync();
                        Console.WriteLine($"Base URL obtida: {baseUrl ?? "null"}");
                    }
                    catch (Exception baseUrlEx)
                    {
                        Console.WriteLine($"ERRO ao buscar Base URL: {baseUrlEx.Message}");
                        _logger.LogError(baseUrlEx, "Erro ao buscar Base URL");
                    }

                    var testResult = new SalesForceAuthTestDto
                    {
                        Authenticated = true,
                        TokenExpiration = authResult.Data?.ExpiresAt,
                        ApiVersion = "v1",
                        ResponseTime = responseTime,
                        BaseUrl = baseUrl,
                        TestedAt = DateTime.UtcNow,
                        CorrelationId = correlationId
                    };

                    Console.WriteLine("=== DADOS DO RESULTADO SUCESSO ===");
                    Console.WriteLine($"Authenticated: {testResult.Authenticated}");
                    Console.WriteLine($"TokenExpiration: {testResult.TokenExpiration}");
                    Console.WriteLine($"ApiVersion: {testResult.ApiVersion}");
                    Console.WriteLine($"ResponseTime: {testResult.ResponseTime}ms");
                    Console.WriteLine($"BaseUrl: {testResult.BaseUrl}");
                    Console.WriteLine($"TestedAt: {testResult.TestedAt}");
                    Console.WriteLine($"CorrelationId: {testResult.CorrelationId}");

                    _logger.LogInformation("Teste de login SalesForce realizado com sucesso. Tempo: {ResponseTime}ms", responseTime);

                    Console.WriteLine("=== RETORNANDO SUCESSO ===");
                    return Ok(ApiResponse<SalesForceAuthTestDto>.SuccessResult(
                        testResult,
                        "Autenticação realizada com sucesso"));
                }
                else
                {
                    Console.WriteLine("=== AUTENTICAÇÃO FALHOU ===");
                    Console.WriteLine($"Mensagem de erro: {authResult.Message}");
                    Console.WriteLine($"Número de erros: {authResult.Errors?.Count ?? 0}");

                    if (authResult.Errors?.Any() == true)
                    {
                        Console.WriteLine("=== ERROS DETALHADOS ===");
                        foreach (var error in authResult.Errors)
                        {
                            Console.WriteLine($"- {error}");
                        }
                    }

                    var testResult = new SalesForceAuthTestDto
                    {
                        Authenticated = false,
                        ResponseTime = responseTime,
                        ErrorMessage = authResult.Message,
                        TestedAt = DateTime.UtcNow,
                        CorrelationId = correlationId
                    };

                    Console.WriteLine("=== DADOS DO RESULTADO ERRO ===");
                    Console.WriteLine($"Authenticated: {testResult.Authenticated}");
                    Console.WriteLine($"ResponseTime: {testResult.ResponseTime}ms");
                    Console.WriteLine($"ErrorMessage: {testResult.ErrorMessage}");
                    Console.WriteLine($"TestedAt: {testResult.TestedAt}");
                    Console.WriteLine($"CorrelationId: {testResult.CorrelationId}");

                    _logger.LogWarning("Teste de login SalesForce falhou. Message: {Message}", authResult.Message);

                    Console.WriteLine("=== RETORNANDO BAD REQUEST (FALHA AUTH) ===");
                    return BadRequest(ApiResponse<SalesForceAuthTestDto>.ErrorResult("Erro durante teste de autenticação"));
                }
            }
            catch (Exception ex)
            {
                var endTime = DateTime.UtcNow;
                var responseTime = (int)(endTime - startTime).TotalMilliseconds;

                Console.WriteLine("=== EXCEÇÃO CAPTURADA ===");
                Console.WriteLine($"Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"Exception Message: {ex.Message}");
                Console.WriteLine($"Inner Exception: {ex.InnerException?.Message ?? "null"}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                Console.WriteLine($"Response Time até erro: {responseTime}ms");

                _logger.LogError(ex, "Erro durante teste de login SalesForce. CorrelationId: {CorrelationId}", correlationId);

                var testResult = new SalesForceAuthTestDto
                {
                    Authenticated = false,
                    ResponseTime = responseTime,
                    ErrorMessage = ex.Message,
                    TestedAt = DateTime.UtcNow,
                    CorrelationId = correlationId
                };

                Console.WriteLine("=== DADOS DO RESULTADO EXCEÇÃO ===");
                Console.WriteLine($"Authenticated: {testResult.Authenticated}");
                Console.WriteLine($"ResponseTime: {testResult.ResponseTime}ms");
                Console.WriteLine($"ErrorMessage: {testResult.ErrorMessage}");
                Console.WriteLine($"TestedAt: {testResult.TestedAt}");
                Console.WriteLine($"CorrelationId: {testResult.CorrelationId}");

                Console.WriteLine("=== RETORNANDO BAD REQUEST (EXCEÇÃO) ===");
                return BadRequest(ApiResponse<SalesForceAuthTestDto>.ErrorResult("Erro interno durante teste de autenticação"));
            }
            finally
            {
                Console.WriteLine($"=== FIM TESTE SALESFORCE ===");
                Console.WriteLine($"CorrelationId: {correlationId}");
                Console.WriteLine($"Timestamp Final: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC");
                Console.WriteLine("==========================================");
            }
        }

        /// <summary>
        /// Verificar status de saúde da API do SalesForce
        /// </summary>
        /// <returns>Status detalhado da API</returns>
        [HttpGet("health")]
        [ProducesResponseType(typeof(ApiResponse<SalesForceHealthDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHealth()
        {
            var correlationId = Guid.NewGuid().ToString();

            _logger.LogInformation("Verificando saúde da API SalesForce. CorrelationId: {CorrelationId}",
                correlationId);

            var healthCheck = await _salesForceService.CheckHealthAsync();

            return Ok(ApiResponse<SalesForceHealthDto>.SuccessResult(
                healthCheck,
                "Status de saúde obtido com sucesso"));
        }

        /// <summary>
        /// Teste completo de integração (autenticação + envio de dados)
        /// </summary>
        /// <param name="request">Dados de teste para envio</param>
        /// <returns>Resultado do teste completo</returns>
        [ProducesResponseType(typeof(ApiResponse<SalesForceIntegrationTestDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [HttpPost("test-integration")]
        public async Task<IActionResult> TestIntegration([FromBody] SalesForceTestDataDto request)
        {
            var correlationId = Guid.NewGuid().ToString();

            _logger.LogInformation("=== TESTE DE INTEGRAÇÃO INICIADO ===");
            _logger.LogInformation("CorrelationId: {CorrelationId}", correlationId);
            _logger.LogInformation("Request Data: {@RequestData}", request);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                _logger.LogError("❌ ModelState inválido: {Errors}", string.Join("; ", errors));
                return BadRequest(ApiResponse<object>.ErrorResult(string.Join("; ", errors)));
            }

            try
            {
                _logger.LogInformation("🔄 Chamando serviço de integração...");
                var result = await _integrationService.TestCompleteIntegrationAsync(request, correlationId);

                _logger.LogInformation("✅ Serviço retornou: Success={Success}", result.Success);

                if (result.Success)
                {
                    _logger.LogInformation("📊 Dados do resultado: {@ResultData}", result.Data);
                    return Ok(result);
                }
                else
                {
                    _logger.LogError("❌ Falha no teste: {Message}", result.Message);
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 EXCEÇÃO no teste de integração");
                return BadRequest(ApiResponse<object>.ErrorResult("Erro interno no teste"));
            }
        }

        /// <summary>
        /// Listar configurações ativas de integração
        /// </summary>
        /// <returns>Lista de configurações</returns>
        [HttpGet("configurations")]
        [ProducesResponseType(typeof(ApiResponse<List<SalesForceConfigDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetConfigurations()
        {
            var configurations = await _integrationService.GetActiveConfigurationsAsync();

            return Ok(ApiResponse<List<SalesForceConfigDto>>.SuccessResult(
                configurations,
                "Configurações obtidas com sucesso"));
        }

        /// <summary>
        /// Validar configuração específica
        /// </summary>
        /// <param name="configId">ID da configuração</param>
        /// <returns>Resultado da validação</returns>
        [HttpPost("validate-config/{configId:int}")]
        [ProducesResponseType(typeof(ApiResponse<SalesForceConfigValidationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ValidateConfiguration(int configId)
        {
            var correlationId = Guid.NewGuid().ToString();

            _logger.LogInformation("Validando configuração {ConfigId}. CorrelationId: {CorrelationId}",
                configId, correlationId);

            var result = await _integrationService.ValidateConfigurationAsync(configId, correlationId);

            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Obter métricas de performance da integração
        /// </summary>
        /// <param name="days">Número de dias para análise (padrão: 7)</param>
        /// <returns>Métricas de performance</returns>
        [HttpGet("metrics")]
        [ProducesResponseType(typeof(ApiResponse<SalesForceMetricsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMetrics([FromQuery] int days = 7)
        {
            var metrics = await _integrationService.GetIntegrationMetricsAsync(days);

            return Ok(ApiResponse<SalesForceMetricsDto>.SuccessResult(
                metrics,
                "Métricas obtidas com sucesso"));
        }

        /// <summary>
        /// Forçar sincronização manual de dados pendentes
        /// </summary>
        /// <returns>Resultado da sincronização</returns>
        [HttpPost("force-sync")]
        [ProducesResponseType(typeof(ApiResponse<SalesForceSyncResultDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ForceSync()
        {
            var correlationId = Guid.NewGuid().ToString();

            _logger.LogInformation("Forçando sincronização manual. CorrelationId: {CorrelationId}",
                correlationId);

            var result = await _integrationService.ForceSyncPendingDataAsync(correlationId);

            return Ok(result);
        }

        /// <summary>
        /// Limpar cache de autenticação
        /// </summary>
        /// <returns>Confirmação da limpeza</returns>
        [HttpPost("clear-auth-cache")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ClearAuthCache()
        {
            _logger.LogInformation("Limpando cache de autenticação SalesForce");

            await _salesForceService.ClearAuthCacheAsync();

            return Ok(ApiResponse<bool>.SuccessResult(
                true,
                "Cache de autenticação limpo com sucesso"));
        }
    }
}