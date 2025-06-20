using API.Constants;
using API.Models.DTOs.Webhook;
using API.Models.Responses;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class WebhookController : ControllerBase
{
    private readonly IIntegrationService _integrationService;
    private readonly IRabbitMQService _rabbitMQService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        IIntegrationService integrationService,
        IRabbitMQService rabbitMQService,
        IConfiguration configuration,
        ILogger<WebhookController> logger)
    {
        _integrationService = integrationService;
        _rabbitMQService = rabbitMQService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Receber callback do SalesForce
    /// </summary>
    /// <param name="payload">Dados do webhook</param>
    /// <returns>Confirmação do processamento</returns>
    [HttpPost("salesforce")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ReceiveSalesForceWebhook([FromBody] SalesForceWebhookDto payload)
    {
        var correlationId = Guid.NewGuid().ToString();
        var requestBody = await GetRequestBodyAsync();

        _logger.LogInformation("Webhook SalesForce recebido. EventType: {EventType}, CorrelationId: {CorrelationId}",
            payload.EventType, correlationId);

        try
        {
            if (!await ValidateWebhookSignatureAsync(requestBody))
            {
                _logger.LogWarning("Assinatura do webhook SalesForce inválida. CorrelationId: {CorrelationId}",
                    correlationId);
                return Unauthorized(ApiResponse<object>.ErrorResult("Assinatura inválida"));
            }

            await ProcessWebhookAsync(payload, correlationId);

            return Ok(ApiResponse<bool>.SuccessResult(true, "Webhook processado com sucesso"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar webhook SalesForce. CorrelationId: {CorrelationId}",
                correlationId);

            return BadRequest(ApiResponse<object>.ErrorResult("Erro interno no processamento"));
        }
    }

    /// <summary>
    /// Receber callback genérico de integração
    /// </summary>
    /// <param name="payload">Dados do webhook</param>
    /// <returns>Confirmação do processamento</returns>
    [HttpPost("integration")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReceiveIntegrationWebhook([FromBody] GenericWebhookDto payload)
    {
        var correlationId = Guid.NewGuid().ToString();

        _logger.LogInformation("Webhook de integração recebido. Source: {Source}, CorrelationId: {CorrelationId}",
            payload.Source, correlationId);

        try
        {
            await ProcessGenericWebhookAsync(payload, correlationId);

            return Ok(ApiResponse<bool>.SuccessResult(true, "Webhook processado com sucesso"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar webhook de integração. CorrelationId: {CorrelationId}",
                correlationId);

            return BadRequest(ApiResponse<object>.ErrorResult("Erro interno no processamento"));
        }
    }

    /// <summary>
    /// Listar logs de webhooks recebidos
    /// </summary>
    /// <param name="page">Página</param>
    /// <param name="pageSize">Tamanho da página</param>
    /// <param name="source">Filtro por origem</param>
    /// <param name="eventType">Filtro por tipo de evento</param>
    /// <param name="startDate">Data inicial</param>
    /// <param name="endDate">Data final</param>
    /// <returns>Lista de logs de webhooks</returns>
    [HttpGet("logs")]
    [ProducesResponseType(typeof(PagedResponse<WebhookLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWebhookLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? source = null,
        [FromQuery] string? eventType = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var filter = new WebhookLogFilterDto
        {
            Page = page,
            PageSize = pageSize,
            Source = source,
            EventType = eventType,
            StartDate = startDate,
            EndDate = endDate
        };

        var result = await _integrationService.GetWebhookLogsAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Buscar log de webhook específico
    /// </summary>
    /// <param name="logId">ID do log</param>
    /// <returns>Detalhes do log</returns>
    [HttpGet("logs/{logId:int}")]
    [ProducesResponseType(typeof(ApiResponse<WebhookLogDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWebhookLogById(int logId)
    {
        var result = await _integrationService.GetWebhookLogByIdAsync(logId);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Reprocessar webhook específico
    /// </summary>
    /// <param name="logId">ID do log do webhook</param>
    /// <returns>Resultado do reprocessamento</returns>
    [HttpPost("logs/{logId:int}/reprocess")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReprocessWebhook(int logId)
    {
        var correlationId = Guid.NewGuid().ToString();

        _logger.LogInformation("Reprocessando webhook {LogId}. CorrelationId: {CorrelationId}",
            logId, correlationId);

        var result = await _integrationService.ReprocessWebhookAsync(logId, correlationId);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Obter estatísticas de webhooks
    /// </summary>
    /// <param name="days">Número de dias para análise</param>
    /// <returns>Estatísticas de webhooks</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ApiResponse<WebhookStatisticsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWebhookStatistics([FromQuery] int days = 7)
    {
        var statistics = await _integrationService.GetWebhookStatisticsAsync(days);

        return Ok(ApiResponse<WebhookStatisticsDto>.SuccessResult(
            statistics,
            "Estatísticas obtidas com sucesso"));
    }

    /// <summary>
    /// Testar webhook (para debugging)
    /// </summary>
    /// <param name="testData">Dados de teste</param>
    /// <returns>Resultado do teste</returns>
    [HttpPost("test")]
    [ProducesResponseType(typeof(ApiResponse<WebhookTestResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> TestWebhook([FromBody] WebhookTestDto testData)
    {
        var correlationId = Guid.NewGuid().ToString();

        _logger.LogInformation("Executando teste de webhook. CorrelationId: {CorrelationId}", correlationId);

        var result = await _integrationService.TestWebhookAsync(testData, correlationId);

        return Ok(result);
    }

    /// <summary>
    /// Configurar webhook de saída
    /// </summary>
    /// <param name="config">Configuração do webhook</param>
    /// <returns>Configuração criada</returns>
    [HttpPost("configure")]
    [ProducesResponseType(typeof(ApiResponse<WebhookConfigDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfigureWebhook([FromBody] CreateWebhookConfigDto config)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponse<object>.ErrorResult(string.Join("; ", errors)));
        }

        var result = await _integrationService.CreateWebhookConfigAsync(config);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(
            nameof(GetWebhookConfig),
            new { configId = result.Data!.Id },
            result);
    }

    /// <summary>
    /// Buscar configuração de webhook
    /// </summary>
    /// <param name="configId">ID da configuração</param>
    /// <returns>Configuração do webhook</returns>
    [HttpGet("configure/{configId:int}")]
    [ProducesResponseType(typeof(ApiResponse<WebhookConfigDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWebhookConfig(int configId)
    {
        var result = await _integrationService.GetWebhookConfigAsync(configId);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Listar configurações de webhook
    /// </summary>
    /// <returns>Lista de configurações</returns>
    [HttpGet("configure")]
    [ProducesResponseType(typeof(ApiResponse<List<WebhookConfigDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWebhookConfigs()
    {
        var configs = await _integrationService.GetWebhookConfigsAsync();

        return Ok(ApiResponse<List<WebhookConfigDto>>.SuccessResult(
            configs,
            "Configurações obtidas com sucesso"));
    }

    /// <summary>
    /// Atualizar configuração de webhook
    /// </summary>
    /// <param name="configId">ID da configuração</param>
    /// <param name="config">Dados para atualização</param>
    /// <returns>Configuração atualizada</returns>
    [HttpPut("configure/{configId:int}")]
    [ProducesResponseType(typeof(ApiResponse<WebhookConfigDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateWebhookConfig(int configId, [FromBody] UpdateWebhookConfigDto config)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponse<object>.ErrorResult(string.Join("; ", errors)));
        }

        var result = await _integrationService.UpdateWebhookConfigAsync(configId, config);

        if (!result.Success)
        {
            return result.Message.Contains("não encontrada")
                ? NotFound(result)
                : BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Deletar configuração de webhook
    /// </summary>
    /// <param name="configId">ID da configuração</param>
    /// <returns>Confirmação da exclusão</returns>
    [HttpDelete("configure/{configId:int}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWebhookConfig(int configId)
    {
        var result = await _integrationService.DeleteWebhookConfigAsync(configId);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    // Métodos privados auxiliares
    private async Task<string> GetRequestBodyAsync()
    {
        Request.EnableBuffering();
        Request.Body.Position = 0;

        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();

        Request.Body.Position = 0;
        return body;
    }

    private async Task<bool> ValidateWebhookSignatureAsync(string payload)
    {
        try
        {
            var signature = Request.Headers["X-Signature"].FirstOrDefault();
            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Webhook recebido sem assinatura");
                return false;
            }

            var secretKey = _configuration["Webhook:SecretKey"];
            if (string.IsNullOrEmpty(secretKey))
            {
                _logger.LogWarning("Chave secreta do webhook não configurada");
                return false;
            }

            var computedSignature = ComputeHmacSha256(payload, secretKey);
            var isValid = signature.Equals($"sha256={computedSignature}", StringComparison.OrdinalIgnoreCase);

            if (!isValid)
            {
                _logger.LogWarning("Assinatura do webhook inválida. Esperado: sha256={Expected}, Recebido: {Received}",
                    computedSignature, signature);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar assinatura do webhook");
            return false;
        }
    }

    private string ComputeHmacSha256(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private async Task ProcessWebhookAsync(SalesForceWebhookDto payload, string correlationId)
    {
        await _integrationService.LogWebhookAsync(new WebhookLogCreateDto
        {
            Source = "SalesForce",
            EventType = payload.EventType,
            Payload = System.Text.Json.JsonSerializer.Serialize(payload),
            CorrelationId = correlationId,
            ReceivedAt = DateTime.UtcNow
        });

        switch (payload.EventType?.ToLowerInvariant())
        {
            case "integration_success":
                await ProcessIntegrationSuccessAsync(payload, correlationId);
                break;

            case "integration_error":
                await ProcessIntegrationErrorAsync(payload, correlationId);
                break;

            case "data_updated":
                await ProcessDataUpdatedAsync(payload, correlationId);
                break;

            default:
                _logger.LogWarning("Tipo de evento desconhecido no webhook: {EventType}", payload.EventType);
                break;
        }
    }

    private async Task ProcessGenericWebhookAsync(GenericWebhookDto payload, string correlationId)
    {
        await _integrationService.LogWebhookAsync(new WebhookLogCreateDto
        {
            Source = payload.Source,
            EventType = payload.EventType,
            Payload = System.Text.Json.JsonSerializer.Serialize(payload),
            CorrelationId = correlationId,
            ReceivedAt = DateTime.UtcNow
        });

        await _rabbitMQService.PublishWebhookMessageAsync(payload, correlationId);
    }

    private async Task ProcessIntegrationSuccessAsync(SalesForceWebhookDto payload, string correlationId)
    {
        if (payload.EntityId.HasValue)
        {
            await _integrationService.UpdateIntegrationStatusAsync(
                payload.EntityId.Value,
                1, 
                payload.ExternalId,
                correlationId);

            await _rabbitMQService.PublishEmailMessageAsync(new
            {
                Type = "integration_success",
                EntityId = payload.EntityId.Value,
                ExternalId = payload.ExternalId,
                Timestamp = payload.Timestamp
            }, correlationId);
        }
    }

    private async Task ProcessIntegrationErrorAsync(SalesForceWebhookDto payload, string correlationId)
    {
        if (payload.EntityId.HasValue)
        {
            await _integrationService.UpdateIntegrationStatusAsync(
                payload.EntityId.Value,
                2, 
                null,
                correlationId,
                payload.Data?.ToString());

            await _rabbitMQService.PublishEmailMessageAsync(new
            {
                Type = "integration_error",
                EntityId = payload.EntityId.Value,
                ErrorMessage = payload.Data?.ToString(),
                Timestamp = payload.Timestamp
            }, correlationId);
        }
    }

    private async Task ProcessDataUpdatedAsync(SalesForceWebhookDto payload, string correlationId)
    {
        if (payload.EntityId.HasValue && payload.ExternalId != null)
        {
            await _integrationService.SyncDataFromExternalAsync(
                payload.EntityId.Value,
                payload.ExternalId,
                payload.Data,
                correlationId);
        }
    }
}