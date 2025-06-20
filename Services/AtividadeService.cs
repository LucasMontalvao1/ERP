using API.Models.DTOs.Atividade;
using API.Models.Entities;
using API.Models.Responses;
using API.Repositories.Interfaces;
using API.Services.Interfaces;
using API.Services.Cache.Interfaces;
using API.Constants;
using AutoMapper;

namespace API.Services
{
    public class AtividadeService : IAtividadeService
    {
        private readonly IAtividadeRepository _atividadeRepository;
        private readonly IIntegrationService _integrationService;
        private readonly IHangfireJobService _hangfireJobService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<AtividadeService> _logger;
        private readonly IMapper _mapper;

        public AtividadeService(
            IAtividadeRepository atividadeRepository,
            IIntegrationService integrationService,
            IHangfireJobService hangfireJobService,
            ICacheService cacheService,
            ILogger<AtividadeService> logger,
            IMapper mapper)
        {
            _atividadeRepository = atividadeRepository;
            _integrationService = integrationService;
            _hangfireJobService = hangfireJobService;
            _cacheService = cacheService;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<ApiResponse<AtividadeResponseDto>> CreateAsync(CreateAtividadeDto dto, int userId)
        {
            try
            {
                _logger.LogInformation("Criando atividade {CodAtiv} pelo usuário {UserId}", dto.CodAtiv, userId);

                if (await _atividadeRepository.ExistsByCodAtivAsync(dto.CodAtiv))
                {
                    return ApiResponse<AtividadeResponseDto>.ErrorResult("Atividade já existe com este código");
                }

                var atividade = new Atividade
                {
                    CodAtiv = dto.CodAtiv.Trim().ToUpper(),
                    Ramo = dto.Ramo.Trim(),
                    PercDesc = dto.PercDesc,
                    CalculaSt = dto.CalculaSt.ToUpper(),
                    StatusSincronizacao = 0, 
                    DataCriacao = DateTime.UtcNow,
                    CriadoPor = userId
                };

                // Salvar no banco local primeiro
                var createdAtividade = await _atividadeRepository.CreateAsync(atividade);

                // Mapear para response
                var response = MapToResponseDto(createdAtividade);

                // Invalidar cache
                await InvalidateAtividadeCache();

                // Agendar sincronização assíncrona se solicitado
                if (dto.ForcaSincronizacao)
                {
                    var correlationId = Guid.NewGuid().ToString();
                    await _hangfireJobService.ScheduleIntegrationJobAsync(new Models.DTOs.Jobs.ScheduleIntegrationJobDto
                    {
                        AtividadeId = dto.CodAtiv,
                        TipoOperacao = 1, 
                        Prioridade = 1 
                    }, correlationId);
                }

                _logger.LogInformation("Atividade {CodAtiv} criada com sucesso", dto.CodAtiv);

                return ApiResponse<AtividadeResponseDto>.SuccessResult(response, "Atividade criada com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar atividade {CodAtiv}", dto.CodAtiv);
                return ApiResponse<AtividadeResponseDto>.ErrorResult("Erro interno ao criar atividade");
            }
        }

        public async Task<ApiResponse<AtividadeResponseDto>> UpdateAsync(string codAtiv, UpdateAtividadeDto dto, int userId)
        {
            try
            {
                _logger.LogInformation("Atualizando atividade {CodAtiv} pelo usuário {UserId}", codAtiv, userId);

                // Buscar atividade existente
                var existingAtividade = await _atividadeRepository.GetByCodAtivAsync(codAtiv);
                if (existingAtividade == null)
                {
                    return ApiResponse<AtividadeResponseDto>.ErrorResult("Atividade não encontrada");
                }

                // Atualizar campos
                existingAtividade.Ramo = dto.Ramo.Trim();
                existingAtividade.PercDesc = dto.PercDesc;
                existingAtividade.CalculaSt = dto.CalculaSt.ToUpper();
                existingAtividade.DataAtualizacao = DateTime.UtcNow;
                existingAtividade.AtualizadoPor = userId;

                // Se houve mudança, marcar para sincronização
                existingAtividade.StatusSincronizacao = 0; // Pendente

                // Salvar no banco
                var updatedAtividade = await _atividadeRepository.UpdateAsync(existingAtividade);

                // Mapear para response
                var response = MapToResponseDto(updatedAtividade);

                // Invalidar cache
                await InvalidateAtividadeCache();
                await _cacheService.RemoveAsync($"atividade:{codAtiv}");

                // Agendar sincronização assíncrona se solicitado
                if (dto.ForcaSincronizacao)
                {
                    var correlationId = Guid.NewGuid().ToString();
                    await _hangfireJobService.ScheduleIntegrationJobAsync(new Models.DTOs.Jobs.ScheduleIntegrationJobDto
                    {
                        AtividadeId = codAtiv,
                        TipoOperacao = 2, 
                        Prioridade = 1 
                    }, correlationId);
                }

                _logger.LogInformation("Atividade {CodAtiv} atualizada com sucesso", codAtiv);

                return ApiResponse<AtividadeResponseDto>.SuccessResult(response, "Atividade atualizada com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar atividade {CodAtiv}", codAtiv);
                return ApiResponse<AtividadeResponseDto>.ErrorResult("Erro interno ao atualizar atividade");
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(string codAtiv, int userId)
        {
            try
            {
                _logger.LogInformation("Deletando atividade {CodAtiv} pelo usuário {UserId}", codAtiv, userId);

                // Verificar se existe
                var existingAtividade = await _atividadeRepository.GetByCodAtivAsync(codAtiv);
                if (existingAtividade == null)
                {
                    return ApiResponse<bool>.ErrorResult("Atividade não encontrada");
                }

                // Agendar exclusão na API externa primeiro
                var correlationId = Guid.NewGuid().ToString();
                await _hangfireJobService.ScheduleIntegrationJobAsync(new Models.DTOs.Jobs.ScheduleIntegrationJobDto
                {
                    AtividadeId = codAtiv,
                    TipoOperacao = 3, 
                    Prioridade = 1 
                }, correlationId);

                var deleted = await _atividadeRepository.DeleteAsync(codAtiv);

                if (deleted)
                {
                    // Invalidar cache
                    await InvalidateAtividadeCache();
                    await _cacheService.RemoveAsync($"atividade:{codAtiv}");

                    _logger.LogInformation("Atividade {CodAtiv} deletada com sucesso", codAtiv);
                    return ApiResponse<bool>.SuccessResult(true, "Atividade deletada com sucesso");
                }

                return ApiResponse<bool>.ErrorResult("Falha ao deletar atividade");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar atividade {CodAtiv}", codAtiv);
                return ApiResponse<bool>.ErrorResult("Erro interno ao deletar atividade");
            }
        }

        public async Task<ApiResponse<AtividadeResponseDto>> GetByCodAtivAsync(string codAtiv)
        {
            try
            {
                // Tentar buscar do cache primeiro
                var cacheKey = $"atividade:{codAtiv}";
                var cached = await _cacheService.GetAsync<AtividadeResponseDto>(cacheKey);
                if (cached != null)
                {
                    return ApiResponse<AtividadeResponseDto>.SuccessResult(cached);
                }

                // Buscar do banco
                var atividade = await _atividadeRepository.GetByCodAtivAsync(codAtiv);
                if (atividade == null)
                {
                    return ApiResponse<AtividadeResponseDto>.ErrorResult("Atividade não encontrada");
                }

                var response = MapToResponseDto(atividade);

                // Armazenar no cache
                await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(ApiConstants.CacheSettings.MediumCacheMinutes));

                return ApiResponse<AtividadeResponseDto>.SuccessResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar atividade {CodAtiv}", codAtiv);
                return ApiResponse<AtividadeResponseDto>.ErrorResult("Erro interno ao buscar atividade");
            }
        }

        public async Task<PagedResponse<AtividadeListDto>> GetPagedAsync(AtividadeFilterDto filter)
        {
            try
            {
                // Gerar chave de cache baseada nos filtros
                var cacheKey = GenerateCacheKey(filter);
                var cached = await _cacheService.GetAsync<PagedResponse<AtividadeListDto>>(cacheKey);
                if (cached != null)
                {
                    return cached;
                }

                // Buscar do banco
                var result = await _atividadeRepository.GetPagedAsync(filter);

                // Armazenar no cache por pouco tempo (dados frequentemente alterados)
                await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(ApiConstants.CacheSettings.ShortCacheMinutes));

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar atividades paginadas");
                return new PagedResponse<AtividadeListDto>
                {
                    Success = false,
                    Message = "Erro interno ao buscar atividades"
                };
            }
        }

        public async Task<ApiResponse<IEnumerable<AtividadeListDto>>> SearchAsync(string searchTerm, int limit = 10)
        {
            try
            {
                var atividades = await _atividadeRepository.SearchAsync(searchTerm, limit);
                var result = atividades.Select(a => new AtividadeListDto
                {
                    CodAtiv = a.CodAtiv,
                    Ramo = a.Ramo,
                    PercDesc = a.PercDesc,
                    CalculaSt = a.CalculaSt,
                    StatusSincronizacao = a.StatusSincronizacao,
                    StatusSincronizacaoDescricao = GetStatusDescription(a.StatusSincronizacao),
                    DataUltimaSincronizacao = a.DataUltimaSincronizacao,
                    DataCriacao = a.DataCriacao
                });

                return ApiResponse<IEnumerable<AtividadeListDto>>.SuccessResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao pesquisar atividades com termo: {SearchTerm}", searchTerm);
                return ApiResponse<IEnumerable<AtividadeListDto>>.ErrorResult("Erro interno na pesquisa");
            }
        }

        public async Task<ApiResponse<BatchSyncResponseDto>> SyncBatchAsync(List<string> codAtivs, string correlationId)
        {
            try
            {
                _logger.LogInformation("Iniciando sincronização em lote de {Count} atividades. CorrelationId: {CorrelationId}",
                    codAtivs.Count, correlationId);

                var batchResult = await _integrationService.ProcessBatchAsync(codAtivs, correlationId);

                var response = new BatchSyncResponseDto
                {
                    TotalProcessados = batchResult.Data?.TotalProcessados ?? 0,
                    CorrelationId = correlationId
                };

                if (batchResult.Data != null)
                {
                    if (response.GetType().GetProperty("SuccessCount") != null)
                    {
                        response.GetType().GetProperty("SuccessCount")?.SetValue(response, batchResult.Data.SuccessCount);
                    }
                    if (response.GetType().GetProperty("FailureCount") != null)
                    {
                        response.GetType().GetProperty("FailureCount")?.SetValue(response, batchResult.Data.FailureCount);
                    }
                }

                return ApiResponse<BatchSyncResponseDto>.SuccessResult(response,
                    batchResult.Success ? "Sincronização em lote concluída com sucesso" : "Sincronização em lote concluída com falhas");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar sincronização em lote. CorrelationId: {CorrelationId}", correlationId);
                return ApiResponse<BatchSyncResponseDto>.ErrorResult("Erro interno ao processar sincronização");
            }
        }

        public async Task<ApiResponse<bool>> ForceSyncAsync(string codAtiv, string correlationId)
        {
            try
            {
                _logger.LogInformation("Forçando sincronização da atividade {CodAtiv}. CorrelationId: {CorrelationId}",
                    codAtiv, correlationId);

                // Verificar se atividade existe
                if (!await _atividadeRepository.ExistsByCodAtivAsync(codAtiv))
                {
                    return ApiResponse<bool>.ErrorResult("Atividade não encontrada");
                }

                // Agendar sincronização imediata
                await _hangfireJobService.ScheduleIntegrationJobAsync(new Models.DTOs.Jobs.ScheduleIntegrationJobDto
                {
                    AtividadeId = codAtiv,
                    TipoOperacao = 2, 
                    Prioridade = 1 
                }, correlationId);

                return ApiResponse<bool>.SuccessResult(true, "Sincronização agendada com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao forçar sincronização da atividade {CodAtiv}", codAtiv);
                return ApiResponse<bool>.ErrorResult("Erro interno ao agendar sincronização");
            }
        }

        public async Task<ApiResponse<object>> GetSyncStatisticsAsync()
        {
            try
            {
                var cacheKey = "atividades:sync_statistics";
                var cached = await _cacheService.GetAsync<object>(cacheKey);
                if (cached != null)
                {
                    return ApiResponse<object>.SuccessResult(cached);
                }

                // Buscar estatísticas de cada status
                var statistics = new
                {
                    Pendente = await _atividadeRepository.GetCountByStatusAsync(0),
                    Sincronizado = await _atividadeRepository.GetCountByStatusAsync(1),
                    Erro = await _atividadeRepository.GetCountByStatusAsync(2),
                    Reprocessando = await _atividadeRepository.GetCountByStatusAsync(3),
                    Cancelado = await _atividadeRepository.GetCountByStatusAsync(4),
                    Total = await _atividadeRepository.GetCountByStatusAsync(0) +
                           await _atividadeRepository.GetCountByStatusAsync(1) +
                           await _atividadeRepository.GetCountByStatusAsync(2) +
                           await _atividadeRepository.GetCountByStatusAsync(3) +
                           await _atividadeRepository.GetCountByStatusAsync(4),
                    UltimaAtualizacao = DateTime.UtcNow
                };

                // Cache por 5 minutos
                await _cacheService.SetAsync(cacheKey, statistics, TimeSpan.FromMinutes(5));

                return ApiResponse<object>.SuccessResult(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar estatísticas de sincronização");
                return ApiResponse<object>.ErrorResult("Erro interno ao buscar estatísticas");
            }
        }

        // Métodos auxiliares privados
        private AtividadeResponseDto MapToResponseDto(Atividade atividade)
        {
            return new AtividadeResponseDto
            {
                CodAtiv = atividade.CodAtiv,
                Ramo = atividade.Ramo,
                PercDesc = atividade.PercDesc,
                CalculaSt = atividade.CalculaSt,
                StatusSincronizacao = atividade.StatusSincronizacao,
                StatusSincronizacaoDescricao = GetStatusDescription(atividade.StatusSincronizacao),
                DataUltimaSincronizacao = atividade.DataUltimaSincronizacao,
                TentativasSincronizacao = atividade.TentativasSincronizacao,
                UltimoErroSincronizacao = atividade.UltimoErroSincronizacao,
                DataCriacao = atividade.DataCriacao,
                DataAtualizacao = atividade.DataAtualizacao
            };
        }

        private string GetStatusDescription(int status)
        {
            return status switch
            {
                0 => "Pendente",
                1 => "Sincronizado",
                2 => "Erro",
                3 => "Reprocessando",
                4 => "Cancelado",
                _ => "Desconhecido"
            };
        }

        private string GenerateCacheKey(AtividadeFilterDto filter)
        {
            return $"atividades:paged:{filter.Page}:{filter.PageSize}:{filter.CodAtiv}:{filter.Ramo}:{filter.StatusSincronizacao}:{filter.OrderBy}:{filter.OrderDirection}";
        }

        private async Task InvalidateAtividadeCache()
        {
            await _cacheService.RemoveByPatternAsync("atividades:*");
        }
    }
}