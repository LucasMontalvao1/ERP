using API.Constants;
using API.Models.DTOs.Atividade;
using API.Models.Responses;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces(ApiConstants.ContentTypes.Json)]
    public class AtividadesController : ControllerBase
    {
        private readonly IAtividadeService _atividadeService;
        private readonly IIntegrationService _integrationService;
        private readonly ILogger<AtividadesController> _logger;

        public AtividadesController(
            IAtividadeService atividadeService,
            IIntegrationService integrationService,
            ILogger<AtividadesController> logger)
        {
            _atividadeService = atividadeService;
            _integrationService = integrationService;
            _logger = logger;
        }

        /// <summary>
        /// Listar atividades com paginação e filtros
        /// </summary>
        /// <param name="filter">Filtros de busca</param>
        /// <returns>Lista paginada de atividades</returns>
        [HttpGet]
        [EnableRateLimiting(ApiConstants.RateLimitPolicies.Default)]
        [ProducesResponseType(typeof(PagedResponse<AtividadeListDto>), ApiConstants.StatusCodes.Ok)]
        [ProducesResponseType(typeof(ApiResponse<object>), ApiConstants.StatusCodes.BadRequest)]
        public async Task<IActionResult> GetActivities([FromQuery] AtividadeFilterDto filter)
        {
            var result = await _atividadeService.GetPagedAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Buscar atividade por código
        /// </summary>
        /// <param name="codAtiv">Código da atividade</param>
        /// <returns>Detalhes da atividade</returns>
        [HttpGet("{codAtiv}")]
        [ProducesResponseType(typeof(ApiResponse<AtividadeResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetActivity(string codAtiv)
        {
            var result = await _atividadeService.GetByCodAtivAsync(codAtiv);

            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Criar nova atividade
        /// </summary>
        /// <param name="dto">Dados da atividade</param>
        /// <returns>Atividade criada</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<AtividadeResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateActivity([FromBody] CreateAtividadeDto dto)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("=== CRIAÇÃO DE ATIVIDADE INICIADA ===");
            _logger.LogInformation("CorrelationId: {CorrelationId}", correlationId);
            _logger.LogInformation("Request Data: {@RequestData}", dto);

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
                _logger.LogInformation("🔄 Extraindo User ID...");
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogError("❌ User ID inválido ou não encontrado");
                    return BadRequest(ApiResponse<object>.ErrorResult("Usuário não autenticado"));
                }

                _logger.LogInformation("🔄 Chamando serviço de atividade...");
                var result = await _atividadeService.CreateAsync(dto, userId);
                _logger.LogInformation("✅ Serviço retornou: Success={Success}", result.Success);

                if (!result.Success)
                {
                    _logger.LogError("❌ Falha na criação: {Message}", result.Message);
                    return BadRequest(result);
                }

                _logger.LogInformation("📊 Atividade criada: {CodAtiv}", result.Data?.CodAtiv);

                _logger.LogInformation("=== INICIANDO INTEGRAÇÃO COM API EXTERNA ===");
                _logger.LogInformation("🔄 CodAtiv: {CodAtiv}", result.Data?.CodAtiv);
                _logger.LogInformation("🔄 CorrelationId: {CorrelationId}", correlationId);

                try
                {
                    _logger.LogInformation("📤 Enviando atividade para API externa...");
                    var integrationStartTime = DateTime.UtcNow;

                    var integrationResult = await _integrationService.ProcessAtividadeAsync(
                        result.Data!.CodAtiv,
                        correlationId,
                        isNewActivity: true  
                    );

                    var integrationTime = (int)(DateTime.UtcNow - integrationStartTime).TotalMilliseconds;
                    _logger.LogInformation("⏱️ Tempo de integração: {IntegrationTime}ms", integrationTime);

                    if (integrationResult.Success)
                    {
                        _logger.LogInformation("✅ INTEGRAÇÃO BEM-SUCEDIDA");
                        _logger.LogInformation("🆔 ExternalId: {ExternalId}", integrationResult.Data?.ExternalId);
                        _logger.LogInformation("📈 ResponseTime: {ResponseTime}ms", integrationResult.Data?.ResponseTime);
                        _logger.LogInformation("🕐 ProcessedAt: {ProcessedAt}", integrationResult.Data?.ProcessedAt);

                        if (result.Data != null)
                        {
                            result.Data.IntegrationInfo = new AtividadeIntegrationInfoDto
                            {
                                IntegrationStatus = "Sincronizado",
                                ExternalId = integrationResult.Data?.ExternalId,
                                LastSyncAt = integrationResult.Data?.ProcessedAt,
                                SyncMessage = integrationResult.Data?.Message,
                                CorrelationId = correlationId
                            };
                        }
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ FALHA NA INTEGRAÇÃO (Atividade criada localmente)");
                        _logger.LogWarning("❌ Erro: {IntegrationError}", integrationResult.Message);

                        if (result.Data != null)
                        {
                            result.Data.IntegrationInfo = new AtividadeIntegrationInfoDto
                            {
                                IntegrationStatus = "Erro na Sincronização",
                                ExternalId = null,
                                LastSyncAt = DateTime.UtcNow,
                                SyncMessage = integrationResult.Message,
                                CorrelationId = correlationId
                            };
                        }

                        _logger.LogWarning("📋 Atividade criada localmente mas requer reprocessamento");
                        _logger.LogWarning("🔄 CodAtiv {CodAtiv} pode ser reprocessado via endpoint de retry", result.Data?.CodAtiv);
                    }
                }
                catch (Exception integrationEx)
                {
                    _logger.LogError(integrationEx, "💥 EXCEÇÃO na integração externa");
                    _logger.LogError("🆔 CodAtiv: {CodAtiv}", result.Data?.CodAtiv);
                    _logger.LogError("🆔 CorrelationId: {CorrelationId}", correlationId);
                    _logger.LogError("📝 Mensagem: {Message}", integrationEx.Message);
                    _logger.LogError("📋 StackTrace: {StackTrace}", integrationEx.StackTrace);

                    if (result.Data != null)
                    {
                        result.Data.IntegrationInfo = new AtividadeIntegrationInfoDto
                        {
                            IntegrationStatus = "Erro na Sincronização",
                            ExternalId = null,
                            LastSyncAt = DateTime.UtcNow,
                            SyncMessage = $"Exceção: {integrationEx.Message}",
                            CorrelationId = correlationId
                        };
                    }

                    _logger.LogInformation("ℹ️ ATIVIDADE CRIADA COM SUCESSO (Integração falhada)");
                    _logger.LogInformation("🔄 Integração pode ser tentada novamente via endpoint de retry");
                }

                _logger.LogInformation("=== RESUMO DA OPERAÇÃO ===");
                _logger.LogInformation("📊 Atividade Local: ✅ CRIADA");
                _logger.LogInformation("🆔 CodAtiv: {CodAtiv}", result.Data?.CodAtiv);
                _logger.LogInformation("📤 Integração Externa: {IntegrationStatus}",
                    result.Data?.IntegrationInfo?.IntegrationStatus ?? "Não processada");
                _logger.LogInformation("🆔 CorrelationId: {CorrelationId}", correlationId);
                _logger.LogInformation("=== FIM DO RESUMO ===");

                return CreatedAtAction(
                    nameof(GetActivity),
                    new { codAtiv = result.Data!.CodAtiv },
                    result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 EXCEÇÃO na criação de atividade");
                _logger.LogError("🆔 CorrelationId: {CorrelationId}", correlationId);
                _logger.LogError("📝 Dados da requisição: {@RequestData}", dto);
                return BadRequest(ApiResponse<object>.ErrorResult("Erro interno durante criação"));
            }
        }

        /// <summary>
        /// Atualizar atividade existente
        /// </summary>
        /// <param name="codAtiv">Código da atividade</param>
        /// <param name="dto">Dados para atualização</param>
        /// <returns>Atividade atualizada</returns>
        [HttpPut("{codAtiv}")]
        [ProducesResponseType(typeof(ApiResponse<AtividadeResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateActivity(string codAtiv, [FromBody] UpdateAtividadeDto dto)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("=== ATUALIZAÇÃO DE ATIVIDADE INICIADA ===");
            _logger.LogInformation("CorrelationId: {CorrelationId}", correlationId);
            _logger.LogInformation("CodAtiv: {CodAtiv}", codAtiv);
            _logger.LogInformation("Request Data: {@RequestData}", dto);

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
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogError("❌ User ID inválido ou não encontrado");
                    return BadRequest(ApiResponse<object>.ErrorResult("Usuário não autenticado"));
                }

                _logger.LogInformation("🔄 Chamando serviço de atualização...");
                var result = await _atividadeService.UpdateAsync(codAtiv, dto, userId);

                if (!result.Success)
                {
                    _logger.LogError("❌ Falha na atualização: {Message}", result.Message);
                    return result.Message.Contains("não encontrada")
                        ? NotFound(result)
                        : BadRequest(result);
                }

                _logger.LogInformation("✅ Atividade {CodAtiv} atualizada pelo usuário {UserId}", codAtiv, userId);

                _logger.LogInformation("=== INICIANDO INTEGRAÇÃO COM API EXTERNA (UPDATE) ===");
                _logger.LogInformation("🔄 CodAtiv: {CodAtiv}", codAtiv);
                _logger.LogInformation("🔄 CorrelationId: {CorrelationId}", correlationId);

                try
                {
                    _logger.LogInformation("📤 Atualizando atividade na API externa...");
                    var integrationStartTime = DateTime.UtcNow;

                    var integrationResult = await _integrationService.ProcessAtividadeAsync(
                        codAtiv,
                        correlationId,
                        isNewActivity: false  
                    );

                    var integrationTime = (int)(DateTime.UtcNow - integrationStartTime).TotalMilliseconds;
                    _logger.LogInformation("⏱️ Tempo de integração: {IntegrationTime}ms", integrationTime);

                    if (integrationResult.Success)
                    {
                        _logger.LogInformation("✅ INTEGRAÇÃO DE ATUALIZAÇÃO BEM-SUCEDIDA");
                        _logger.LogInformation("🆔 ExternalId: {ExternalId}", integrationResult.Data?.ExternalId);

                        if (result.Data != null)
                        {
                            result.Data.IntegrationInfo = new AtividadeIntegrationInfoDto
                            {
                                IntegrationStatus = "Sincronizado",
                                ExternalId = integrationResult.Data?.ExternalId,
                                LastSyncAt = integrationResult.Data?.ProcessedAt,
                                SyncMessage = integrationResult.Data?.Message,
                                CorrelationId = correlationId
                            };
                        }
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ FALHA NA INTEGRAÇÃO DE ATUALIZAÇÃO (Atividade atualizada localmente)");
                        _logger.LogWarning("❌ Erro: {IntegrationError}", integrationResult.Message);

                        if (result.Data != null)
                        {
                            result.Data.IntegrationInfo = new AtividadeIntegrationInfoDto
                            {
                                IntegrationStatus = "Erro na Sincronização",
                                ExternalId = null,
                                LastSyncAt = DateTime.UtcNow,
                                SyncMessage = integrationResult.Message,
                                CorrelationId = correlationId
                            };
                        }
                    }
                }
                catch (Exception integrationEx)
                {
                    _logger.LogError(integrationEx, "💥 EXCEÇÃO na integração de atualização");
                    _logger.LogError("🆔 CodAtiv: {CodAtiv}", codAtiv);
                    _logger.LogError("🆔 CorrelationId: {CorrelationId}", correlationId);

                    if (result.Data != null)
                    {
                        result.Data.IntegrationInfo = new AtividadeIntegrationInfoDto
                        {
                            IntegrationStatus = "Erro na Sincronização",
                            ExternalId = null,
                            LastSyncAt = DateTime.UtcNow,
                            SyncMessage = $"Exceção: {integrationEx.Message}",
                            CorrelationId = correlationId
                        };
                    }
                }

                _logger.LogInformation("=== RESUMO DA ATUALIZAÇÃO ===");
                _logger.LogInformation("📊 Atividade Local: ✅ ATUALIZADA");
                _logger.LogInformation("🆔 CodAtiv: {CodAtiv}", codAtiv);
                _logger.LogInformation("📤 Integração Externa: {IntegrationStatus}",
                    result.Data?.IntegrationInfo?.IntegrationStatus ?? "Não processada");
                _logger.LogInformation("🆔 CorrelationId: {CorrelationId}", correlationId);
                _logger.LogInformation("=== FIM DO RESUMO ===");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 EXCEÇÃO na atualização de atividade");
                _logger.LogError("🆔 CorrelationId: {CorrelationId}", correlationId);
                _logger.LogError("🆔 CodAtiv: {CodAtiv}", codAtiv);
                return BadRequest(ApiResponse<object>.ErrorResult("Erro interno durante atualização"));
            }
        }

        /// <summary>
        /// Deletar atividade
        /// </summary>
        /// <param name="codAtiv">Código da atividade</param>
        /// <returns>Confirmação da exclusão</returns>
        [HttpDelete("{codAtiv}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteActivity(string codAtiv)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("=== EXCLUSÃO DE ATIVIDADE INICIADA ===");
            _logger.LogInformation("CorrelationId: {CorrelationId}", correlationId);
            _logger.LogInformation("CodAtiv: {CodAtiv}", codAtiv);

            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogError("❌ User ID inválido ou não encontrado");
                    return BadRequest(ApiResponse<object>.ErrorResult("Usuário não autenticado"));
                }

                var result = await _atividadeService.DeleteAsync(codAtiv, userId);

                if (!result.Success)
                {
                    _logger.LogError("❌ Falha na exclusão: {Message}", result.Message);
                    return NotFound(result);
                }

                _logger.LogInformation("✅ Atividade {CodAtiv} deletada pelo usuário {UserId}", codAtiv, userId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 EXCEÇÃO na exclusão de atividade");
                _logger.LogError("🆔 CorrelationId: {CorrelationId}", correlationId);
                _logger.LogError("🆔 CodAtiv: {CodAtiv}", codAtiv);
                return BadRequest(ApiResponse<object>.ErrorResult("Erro interno durante exclusão"));
            }
        }

        /// <summary>
        /// Pesquisar atividades por termo
        /// </summary>
        /// <param name="q">Termo de busca</param>
        /// <param name="limit">Limite de resultados (padrão: 10)</param>
        /// <returns>Lista de atividades encontradas</returns>
        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<AtividadeListDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchActivities([FromQuery] string q, [FromQuery] int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Termo de busca deve ter pelo menos 2 caracteres"));
            }

            var result = await _atividadeService.SearchAsync(q, limit);
            return Ok(result);
        }

        /// <summary>
        /// Sincronizar atividades em lote com API externa
        /// </summary>
        /// <param name="request">Lista de códigos de atividades para sincronizar</param>
        /// <returns>Resultado da sincronização em lote</returns>
        [HttpPost("sync/batch")]
        [ProducesResponseType(typeof(ApiResponse<BatchSyncResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SyncBatch([FromBody] BatchSyncRequestDto request)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("=== SINCRONIZAÇÃO EM LOTE INICIADA ===");
            _logger.LogInformation("CorrelationId: {CorrelationId}", correlationId);
            _logger.LogInformation("Total de atividades: {Count}", request.CodAtivs.Count);

            if (!ModelState.IsValid || !request.CodAtivs.Any())
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Lista de códigos de atividades é obrigatória"));
            }

            if (request.CodAtivs.Count > 100)
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Máximo de 100 atividades por lote"));
            }

            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogError("❌ User ID inválido ou não encontrado");
                    return BadRequest(ApiResponse<object>.ErrorResult("Usuário não autenticado"));
                }

                _logger.LogInformation("Iniciando sincronização em lote de {Count} atividades pelo usuário {UserId}. CorrelationId: {CorrelationId}",
                    request.CodAtivs.Count, userId, correlationId);

                // ✅ USAR INTEGRATION SERVICE PARA LOTE
                var integrationResult = await _integrationService.ProcessBatchAsync(request.CodAtivs, correlationId);

                if (integrationResult.Success)
                {
                    _logger.LogInformation("✅ Sincronização em lote concluída com sucesso");
                    _logger.LogInformation("📊 Total processados: {Total}", integrationResult.Data?.TotalProcessados);
                    _logger.LogInformation("✅ Sucessos: {Success}", integrationResult.Data?.SuccessCount);
                    _logger.LogInformation("❌ Falhas: {Failures}", integrationResult.Data?.FailureCount);
                }
                else
                {
                    _logger.LogError("❌ Falha na sincronização em lote: {Error}", integrationResult.Message);
                }

                return Ok(integrationResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 EXCEÇÃO na sincronização em lote");
                _logger.LogError("🆔 CorrelationId: {CorrelationId}", correlationId);
                return BadRequest(ApiResponse<object>.ErrorResult("Erro interno na sincronização em lote"));
            }
        }

        /// <summary>
        /// Forçar sincronização individual de uma atividade
        /// </summary>
        /// <param name="codAtiv">Código da atividade</param>
        /// <returns>Confirmação da sincronização</returns>
        [HttpPost("{codAtiv}/sync")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ForceSync(string codAtiv)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("=== FORÇA SINCRONIZAÇÃO INICIADA ===");
            _logger.LogInformation("CorrelationId: {CorrelationId}", correlationId);
            _logger.LogInformation("CodAtiv: {CodAtiv}", codAtiv);

            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogError("❌ User ID inválido ou não encontrado");
                    return BadRequest(ApiResponse<object>.ErrorResult("Usuário não autenticado"));
                }

                _logger.LogInformation("Forçando sincronização da atividade {CodAtiv} pelo usuário {UserId}. CorrelationId: {CorrelationId}",
                    codAtiv, userId, correlationId);

                // ✅ USAR INTEGRATION SERVICE PARA RETRY
                var integrationResult = await _integrationService.RetryIntegrationAsync(codAtiv);

                if (integrationResult.Success)
                {
                    _logger.LogInformation("✅ Força sincronização bem-sucedida");
                    _logger.LogInformation("🆔 ExternalId: {ExternalId}", integrationResult.Data?.ExternalId);
                    _logger.LogInformation("📈 ResponseTime: {ResponseTime}ms", integrationResult.Data?.ResponseTime);

                    var successResult = ApiResponse<object>.SuccessResult(new
                    {
                        synchronized = true,
                        codAtiv = codAtiv,
                        externalId = integrationResult.Data?.ExternalId,
                        message = integrationResult.Data?.Message,
                        correlationId = correlationId,
                        processedAt = integrationResult.Data?.ProcessedAt
                    }, "Sincronização forçada executada com sucesso");

                    return Ok(successResult);
                }
                else
                {
                    _logger.LogError("❌ Falha na força sincronização: {Error}", integrationResult.Message);

                    var errorResult = ApiResponse<object>.ErrorResult(integrationResult.Message ?? "Erro na sincronização forçada");
                    return BadRequest(errorResult);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 EXCEÇÃO na força sincronização");
                _logger.LogError("🆔 CorrelationId: {CorrelationId}", correlationId);
                _logger.LogError("🆔 CodAtiv: {CodAtiv}", codAtiv);
                return BadRequest(ApiResponse<object>.ErrorResult("Erro interno na sincronização forçada"));
            }
        }

        /// <summary>
        /// Obter estatísticas de sincronização
        /// </summary>
        /// <returns>Estatísticas dos status de sincronização</returns>
        [HttpGet("sync/statistics")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSyncStatistics()
        {
            try
            {
                var integrationStats = await _integrationService.GetIntegrationStatisticsAsync(7);
                var atividadeStats = await _atividadeService.GetSyncStatisticsAsync();

                var combinedStats = new
                {
                    AtividadeStats = atividadeStats.Data,
                    IntegrationStats = integrationStats.Data,
                    GeneratedAt = DateTime.UtcNow
                };

                return Ok(ApiResponse<object>.SuccessResult(combinedStats));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 EXCEÇÃO ao obter estatísticas de sincronização");
                return BadRequest(ApiResponse<object>.ErrorResult("Erro interno ao obter estatísticas"));
            }
        }

        /// <summary>
        /// Exportar atividades para CSV
        /// </summary>
        /// <param name="filter">Filtros para exportação</param>
        /// <returns>Arquivo CSV</returns>
        [HttpGet("export/csv")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportToCsv([FromQuery] AtividadeFilterDto filter)
        {
            try
            {
                filter.PageSize = int.MaxValue;
                var result = await _atividadeService.GetPagedAsync(filter);

                if (!result.Success || result.Data == null)
                {
                    return BadRequest("Erro ao buscar dados para exportação");
                }

                var csv = GenerateCsv(result.Data);
                var bytes = System.Text.Encoding.UTF8.GetBytes(csv);

                var fileName = $"atividades_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                Response.Headers.Add("Content-Disposition", $"attachment; filename={fileName}");

                _logger.LogInformation("Exportação CSV de {Count} atividades realizada", result.Data.Count());

                return File(bytes, ApiConstants.ContentTypes.Csv, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao exportar atividades para CSV");
                return StatusCode(500, "Erro interno ao exportar dados");
            }
        }

        // Métodos auxiliares privados
        private string GenerateCsv(IEnumerable<AtividadeListDto> atividades)
        {
            var csv = new System.Text.StringBuilder();

            csv.AppendLine("Codigo,Ramo,PercentualDesconto,CalculaST,StatusSincronizacao,DataUltimaSincronizacao,DataCriacao");

            foreach (var atividade in atividades)
            {
                csv.AppendLine($"{atividade.CodAtiv},{atividade.Ramo},{atividade.PercDesc},{atividade.CalculaSt},{atividade.StatusSincronizacaoDescricao},{atividade.DataUltimaSincronizacao:yyyy-MM-dd HH:mm:ss},{atividade.DataCriacao:yyyy-MM-dd HH:mm:ss}");
            }

            return csv.ToString();
        }
    }

    /// <summary>
    /// DTO para solicitação de sincronização em lote
    /// </summary>
    public class BatchSyncRequestDto
    {
        [Required]
        public List<string> CodAtivs { get; set; } = new();
    }
}