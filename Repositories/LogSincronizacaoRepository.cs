using API.Infra.Data;
using API.Models.DTOs.Integration;
using API.Models.Entities;
using API.Models.Responses;
using API.Repositories.Base;
using API.Repositories.Interfaces;
using API.SQL;
using API.Helpers;
using MySqlConnector;
using System.Data;

namespace API.Repositories
{
    public class LogSincronizacaoRepository : BaseRepository<LogSincronizacao>, ILogSincronizacaoRepository
    {
        private readonly SqlLoader _sqlLoader;

        public LogSincronizacaoRepository(
            IDatabaseService databaseService,
            ILogger<LogSincronizacaoRepository> logger,
            SqlLoader sqlLoader)
            : base(databaseService, logger)
        {
            _sqlLoader = sqlLoader;
        }

        protected override string TableName => "logs_sincronizacao";
        protected override string IdColumn => "id";

        protected override LogSincronizacao MapFromDataRow(DataRow row)
        {
            return new LogSincronizacao
            {
                Id = Convert.ToInt32(row["id"]),
                ConfiguracaoId = Convert.ToInt32(row["configuracaoid"]),
                CodAtiv = row["codativ"].ToString() ?? string.Empty,
                TipoOperacao = Convert.ToInt32(row["tipooperacao"]),
                StatusProcessamento = Convert.ToInt32(row["statusprocessamento"]),
                CategoriaEndpoint = row["categoriaendpoint"].ToString(),
                AcaoEndpoint = row["acaoendpoint"].ToString(),
                EndpointUsado = row["endpointusado"].ToString(),
                MetodoHttpUsado = row["metodohttpusado"].ToString(),
                PayloadEnviado = row["payloadenviado"].ToString(),
                RespostaRecebida = row["respostarecebida"].ToString(),
                CodigoHttp = row["codigohttp"] == DBNull.Value ? null : Convert.ToInt32(row["codigo_http"]),
                MensagemErro = row["mensagemerro"].ToString(),
                TempoProcessamentoMs = Convert.ToInt64(row["tempoprocessamentoms"]),
                NumeroTentativa = Convert.ToInt32(row["numerotentativa"]),
                ProximaTentativa = row["proximatentativa"] == DBNull.Value
                    ? null : Convert.ToDateTime(row["proximatentativa"]),
                JobId = row["jobid"].ToString(),
                Metadados = row["metadados"].ToString(),
                CorrelationId = row["correlationid"].ToString(),
                UserAgent = row["useragent"].ToString(),
                IpOrigem = row["iporigem"].ToString(),
                TamanhoPayloadBytes = row["tamanhopayloadbytes"] == DBNull.Value
                    ? null : Convert.ToInt32(row["tamanhopayloadbytes"]),
                TamanhoRespostaBytes = row["tamanhorespostabytes"] == DBNull.Value
                    ? null : Convert.ToInt32(row["tamanhorespostabytes"]),
                DataCriacao = Convert.ToDateTime(row["datacriacao"]),
                DataAtualizacao = row["dataatualizacao"] == DBNull.Value
                    ? null : Convert.ToDateTime(row["dataatualizacao"]),
                Version = Convert.ToDateTime(row["version"])
            };
        }

        protected override MySqlParameter[] GetInsertParameters(LogSincronizacao entity)
        {
            return new[]
            {
                new MySqlParameter("@configuracaoId", entity.ConfiguracaoId),
                new MySqlParameter("@codAtiv", entity.CodAtiv),
                new MySqlParameter("@tipoOperacao", entity.TipoOperacao),
                new MySqlParameter("@statusProcessamento", entity.StatusProcessamento),
                new MySqlParameter("@categoriaEndpoint", entity.CategoriaEndpoint ?? (object)DBNull.Value),
                new MySqlParameter("@acaoEndpoint", entity.AcaoEndpoint ?? (object)DBNull.Value),
                new MySqlParameter("@endpointUsado", entity.EndpointUsado ?? (object)DBNull.Value),
                new MySqlParameter("@metodoHttpUsado", entity.MetodoHttpUsado ?? (object)DBNull.Value),
                new MySqlParameter("@payloadEnviado", entity.PayloadEnviado ?? (object)DBNull.Value),
                new MySqlParameter("@codigoHttp", entity.CodigoHttp ?? (object)DBNull.Value),
                new MySqlParameter("@tempoProcessamentoMs", entity.TempoProcessamentoMs),
                new MySqlParameter("@numeroTentativa", entity.NumeroTentativa),
                new MySqlParameter("@jobId", entity.JobId ?? (object)DBNull.Value),
                new MySqlParameter("@metadados", entity.Metadados ?? (object)DBNull.Value),
                new MySqlParameter("@correlationId", entity.CorrelationId ?? (object)DBNull.Value),
                new MySqlParameter("@userAgent", entity.UserAgent ?? (object)DBNull.Value),
                new MySqlParameter("@ipOrigem", entity.IpOrigem ?? (object)DBNull.Value),
                new MySqlParameter("@tamanhoPayloadBytes", entity.TamanhoPayloadBytes ?? (object)DBNull.Value),
                new MySqlParameter("@dataCriacao", entity.DataCriacao)
            };
        }

        protected override MySqlParameter[] GetUpdateParameters(LogSincronizacao entity)
        {
            return new[]
            {
                new MySqlParameter("@id", entity.Id),
                new MySqlParameter("@statusProcessamento", entity.StatusProcessamento),
                new MySqlParameter("@respostaRecebida", entity.RespostaRecebida ?? (object)DBNull.Value),
                new MySqlParameter("@codigoHttp", entity.CodigoHttp ?? (object)DBNull.Value),
                new MySqlParameter("@mensagemErro", entity.MensagemErro ?? (object)DBNull.Value),
                new MySqlParameter("@tempoProcessamentoMs", entity.TempoProcessamentoMs),
                new MySqlParameter("@numeroTentativa", entity.NumeroTentativa),
                new MySqlParameter("@proximaTentativa", entity.ProximaTentativa ?? (object)DBNull.Value),
                new MySqlParameter("@tamanhoRespostaBytes", entity.TamanhoRespostaBytes ?? (object)DBNull.Value),
                new MySqlParameter("@dataAtualizacao", DateTime.UtcNow)
            };
        }

        protected override void SetEntityId(LogSincronizacao entity, int id)
        {
            entity.Id = id;
        }

        public async Task<LogSincronizacao> CreateLogAsync(LogSincronizacao log)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Logs_sincronizacao", "CreateLog");
                var parameters = GetInsertParameters(log);

                var newId = await _databaseService.ExecuteScalarAsync(sql, parameters);
                log.Id = Convert.ToInt32(newId);

                _logger.LogInformation("Log de sincronização criado com ID: {LogId}", log.Id);
                return log;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar log de sincronização");
                throw;
            }
        }

        public async Task<bool> UpdateLogStatusAsync(int logId, int status, string? response = null, string? errorMessage = null)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Logs_sincronizacao", "UpdateLogStatus");
                var parameters = new[]
                {
                    new MySqlParameter("@logId", logId),
                    new MySqlParameter("@status", status),
                    new MySqlParameter("@response", response ?? (object)DBNull.Value),
                    new MySqlParameter("@errorMessage", errorMessage ?? (object)DBNull.Value),
                    new MySqlParameter("@dataAtualizacao", DateTime.UtcNow)
                };

                var rowsAffected = await _databaseService.ExecuteNonQueryAsync(sql, parameters);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation("Status do log {LogId} atualizado para {Status}", logId, status);
                }

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar status do log: {LogId}", logId);
                throw;
            }
        }

        public async Task<PagedResponse<SyncLogDto>> GetPagedLogsAsync(SyncLogFilterDto filter)
        {
            try
            {
                var (page, pageSize) = ValidationHelper.ValidatePagination(filter.Page, filter.PageSize);
                var offset = (page - 1) * pageSize;

                var parameters = new[]
                {
                    new MySqlParameter("@codAtiv", string.IsNullOrEmpty(filter.CodAtiv) ? DBNull.Value : filter.CodAtiv),
                    new MySqlParameter("@tipoOperacao", filter.TipoOperacao?.ToString() ?? (object)DBNull.Value),
                    new MySqlParameter("@statusProcessamento", filter.StatusProcessamento?.ToString() ?? (object)DBNull.Value),
                    new MySqlParameter("@dataInicio", filter.DataInicio ?? (object)DBNull.Value),
                    new MySqlParameter("@dataFim", filter.DataFim ?? (object)DBNull.Value),
                    new MySqlParameter("@correlationId", string.IsNullOrEmpty(filter.CorrelationId) ? DBNull.Value : filter.CorrelationId),
                    new MySqlParameter("@pageSize", pageSize),
                    new MySqlParameter("@offset", offset)
                };

                // Contar total
                var countSql = _sqlLoader.GetQuery("Logs_sincronizacao", "CountPagedLogs");
                var totalCount = Convert.ToInt32(await _databaseService.ExecuteScalarAsync(countSql, parameters.Take(6).ToArray()));

                // Buscar dados
                var dataSql = _sqlLoader.GetQuery("Logs_sincronizacao", "GetPagedLogs");
                var dataTable = await _databaseService.ExecuteQueryAsync(dataSql, parameters);

                var items = new List<SyncLogDto>();
                foreach (DataRow row in dataTable.Rows)
                {
                    items.Add(MapToSyncLogDto(row));
                }

                return PagedResponse<SyncLogDto>.Create(
                    items,
                    page,
                    pageSize,
                    totalCount,
                    "Logs recuperados com sucesso"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar logs paginados");
                throw;
            }
        }

        public async Task<LogSincronizacao?> GetByCorrelationIdAsync(string correlationId)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Logs_sincronizacao", "GetByCorrelationId");
                var parameters = new[] { new MySqlParameter("@correlationId", correlationId) };

                var dataTable = await _databaseService.ExecuteQueryAsync(sql, parameters);

                if (dataTable.Rows.Count == 0)
                    return null;

                return MapFromDataRow(dataTable.Rows[0]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar log por correlation ID: {CorrelationId}", correlationId);
                throw;
            }
        }

        public async Task<IEnumerable<LogSincronizacao>> GetFailedLogsAsync(int limit = 50)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Logs_sincronizacao", "GetFailedLogs");
                var parameters = new[] { new MySqlParameter("@limit", limit) };

                var dataTable = await _databaseService.ExecuteQueryAsync(sql, parameters);
                var result = new List<LogSincronizacao>();

                foreach (DataRow row in dataTable.Rows)
                {
                    result.Add(MapFromDataRow(row));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar logs com falha");
                throw;
            }
        }

        public async Task<IEnumerable<LogSincronizacao>> GetByAtividadeAsync(string codAtiv)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Logs_sincronizacao", "GetByAtividade");
                var parameters = new[] { new MySqlParameter("@codAtiv", codAtiv) };

                var dataTable = await _databaseService.ExecuteQueryAsync(sql, parameters);
                var result = new List<LogSincronizacao>();

                foreach (DataRow row in dataTable.Rows)
                {
                    result.Add(MapFromDataRow(row));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar logs por atividade: {CodAtiv}", codAtiv);
                throw;
            }
        }

        public async Task<IntegrationStatisticsDto> GetStatisticsAsync(int days = 7)
        {
            try
            {
                var statistics = new IntegrationStatisticsDto
                {
                    PeriodoInicio = DateTime.UtcNow.AddDays(-days),
                    PeriodoFim = DateTime.UtcNow
                };

                // Estatísticas gerais
                var generalSql = _sqlLoader.GetQuery("Logs_sincronizacao", "GetStatistics");
                var generalParams = new[] { new MySqlParameter("@days", days) };
                var generalData = await _databaseService.ExecuteQueryAsync(generalSql, generalParams);

                foreach (DataRow row in generalData.Rows)
                {
                    var status = Convert.ToInt32(row["statusprocessamento"]);
                    var total = Convert.ToInt32(row["total"]);

                    statistics.TotalIntegracoes += total;

                    switch (status)
                    {
                        case 1: statistics.IntegracoesSucesso = total; break;
                        case 2: statistics.IntegracoesErro = total; break;
                        case 0: statistics.IntegracoesPendentes = total; break;
                    }
                }

                statistics.TaxaSucesso = statistics.TotalIntegracoes > 0
                    ? (double)statistics.IntegracoesSucesso / statistics.TotalIntegracoes * 100
                    : 0;

                // Tempo médio de processamento
                var avgTimeSql = _sqlLoader.GetQuery("Logs_sincronizacao", "GetAverageProcessingTime");
                var avgTimeResult = await _databaseService.ExecuteScalarAsync(avgTimeSql, generalParams);
                statistics.TempoMedioProcessamento = Convert.ToInt64(avgTimeResult ?? 0);

                // Estatísticas diárias
                var dailySql = _sqlLoader.GetQuery("Logs_sincronizacao", "GetDailyStatistics");
                var dailyData = await _databaseService.ExecuteQueryAsync(dailySql, generalParams);

                foreach (DataRow row in dailyData.Rows)
                {
                    statistics.Diario.Add(new IntegrationStatisticDaily
                    {
                        Data = Convert.ToDateTime(row["data"]),
                        Total = Convert.ToInt32(row["total"]),
                        Sucesso = Convert.ToInt32(row["sucesso"]),
                        Erro = Convert.ToInt32(row["erro"]),
                        Pendente = Convert.ToInt32(row["pendente"])
                    });
                }

                // Estatísticas por operação
                var operationSql = _sqlLoader.GetQuery("Logs_sincronizacao", "GetStatisticsByOperation");
                var operationData = await _databaseService.ExecuteQueryAsync(operationSql, generalParams);

                foreach (DataRow row in operationData.Rows)
                {
                    var total = Convert.ToInt32(row["total"]);
                    var sucesso = Convert.ToInt32(row["sucesso"]);

                    statistics.PorOperacao.Add(new IntegrationStatisticByOperation
                    {
                        TipoOperacao = row["tipooperacaodesc"].ToString() ?? string.Empty,
                        Total = total,
                        Sucesso = sucesso,
                        Erro = Convert.ToInt32(row["erro"]),
                        TaxaSucesso = total > 0 ? (double)sucesso / total * 100 : 0
                    });
                }

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar estatísticas de integração");
                throw;
            }
        }

        public async Task<bool> CleanupOldLogsAsync(int olderThanDays = 30)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Logs_sincronizacao", "CleanupOldLogs");
                var parameters = new[] { new MySqlParameter("@olderThanDays", olderThanDays) };

                var rowsAffected = await _databaseService.ExecuteNonQueryAsync(sql, parameters);

                _logger.LogInformation("Limpeza de logs antigos concluída. Registros removidos: {Count}", rowsAffected);

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao limpar logs antigos");
                throw;
            }
        }

        public async Task<int> GetCountByStatusAsync(int status, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Logs_sincronizacao", "GetCountByStatus");
                var parameters = new[]
                {
                    new MySqlParameter("@status", status),
                    new MySqlParameter("@startDate", startDate ?? (object)DBNull.Value),
                    new MySqlParameter("@endDate", endDate ?? (object)DBNull.Value)
                };

                var count = await _databaseService.ExecuteScalarAsync(sql, parameters);
                return Convert.ToInt32(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao contar logs por status: {Status}", status);
                throw;
            }
        }

        public async Task<IEnumerable<LogSincronizacao>> GetRecentByStatusAsync(int status, int limit = 10)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Logs_sincronizacao", "GetRecentByStatus");
                var parameters = new[]
                {
                    new MySqlParameter("@status", status),
                    new MySqlParameter("@limit", limit)
                };

                var dataTable = await _databaseService.ExecuteQueryAsync(sql, parameters);
                var result = new List<LogSincronizacao>();

                foreach (DataRow row in dataTable.Rows)
                {
                    result.Add(MapFromDataRow(row));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar logs recentes por status: {Status}", status);
                throw;
            }
        }

        public async Task<bool> UpdateRetryInfoAsync(int logId, int numeroTentativa, DateTime? proximaTentativa)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Logs_sincronizacao", "UpdateRetryInfo");
                var parameters = new[]
                {
                    new MySqlParameter("@logId", logId),
                    new MySqlParameter("@numeroTentativa", numeroTentativa),
                    new MySqlParameter("@proximaTentativa", proximaTentativa ?? (object)DBNull.Value),
                    new MySqlParameter("@dataAtualizacao", DateTime.UtcNow)
                };

                var rowsAffected = await _databaseService.ExecuteNonQueryAsync(sql, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar informações de retry do log: {LogId}", logId);
                throw;
            }
        }

        public async Task<IEnumerable<LogSincronizacao>> GetLogsForRetryAsync(int maxRetries = 3)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Logs_sincronizacao", "GetLogsForRetry");
                var parameters = new[]
                {
                    new MySqlParameter("@maxRetries", maxRetries),
                    new MySqlParameter("@currentTime", DateTime.UtcNow)
                };

                var dataTable = await _databaseService.ExecuteQueryAsync(sql, parameters);
                var result = new List<LogSincronizacao>();

                foreach (DataRow row in dataTable.Rows)
                {
                    result.Add(MapFromDataRow(row));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar logs para retry");
                throw;
            }
        }

        public async Task<Dictionary<string, object>> GetPerformanceMetricsAsync(int days = 30)
        {
            try
            {
                var metrics = new Dictionary<string, object>();

                var sql = _sqlLoader.GetQuery("Logs_sincronizacao", "GetPerformanceMetrics");
                var parameters = new[] { new MySqlParameter("@days", days) };

                var dataTable = await _databaseService.ExecuteQueryAsync(sql, parameters);

                if (dataTable.Rows.Count > 0)
                {
                    var row = dataTable.Rows[0];
                    metrics["TotalRequests"] = Convert.ToInt32(row["total_requests"]);
                    metrics["AverageResponseTime"] = Convert.ToDouble(row["avg_response_time"]);
                    metrics["MinResponseTime"] = Convert.ToInt64(row["min_response_time"]);
                    metrics["MaxResponseTime"] = Convert.ToInt64(row["max_response_time"]);
                    metrics["SuccessRate"] = Convert.ToDouble(row["success_rate"]);
                    metrics["ErrorRate"] = Convert.ToDouble(row["error_rate"]);
                    metrics["MedianResponseTime"] = Convert.ToDouble(row["median_response_time"]);
                }

                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar métricas de performance");
                throw;
            }
        }

        // Métodos auxiliares privados
        private SyncLogDto MapToSyncLogDto(DataRow row)
        {
            return new SyncLogDto
            {
                Id = Convert.ToInt32(row["id"]),
                CodAtiv = row["cod_ativ"].ToString() ?? string.Empty,
                TipoOperacao = row["tipo_operacao_desc"].ToString() ?? string.Empty,
                Status = row["status_desc"].ToString() ?? string.Empty,
                EndpointUsado = row["endpoint_usado"].ToString(),
                MetodoHttp = row["metodo_http_usado"].ToString(),
                CodigoHttp = row["codigo_http"] == DBNull.Value ? null : Convert.ToInt32(row["codigo_http"]),
                MensagemErro = row["mensagem_erro"].ToString(),
                TempoProcessamento = Convert.ToInt64(row["tempo_processamento_ms"]),
                NumeroTentativa = Convert.ToInt32(row["numero_tentativa"]),
                ProximaTentativa = row["proxima_tentativa"] == DBNull.Value
                    ? null : Convert.ToDateTime(row["proxima_tentativa"]),
                CorrelationId = row["correlation_id"].ToString(),
                DataCriacao = Convert.ToDateTime(row["data_criacao"])
            };
        }
    }
}