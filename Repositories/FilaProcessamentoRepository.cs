using API.Infra.Data;
using API.Models.Entities;
using API.Repositories.Base;
using API.Repositories.Interfaces;
using API.SQL;
using MySqlConnector;
using System.Data;

namespace API.Repositories
{
    public class FilaProcessamentoRepository : BaseRepository<FilaProcessamento>, IFilaProcessamentoRepository
    {
        private readonly SqlLoader _sqlLoader;

        public FilaProcessamentoRepository(
            IDatabaseService databaseService,
            ILogger<FilaProcessamentoRepository> logger,
            SqlLoader sqlLoader)
            : base(databaseService, logger)
        {
            _sqlLoader = sqlLoader;
        }

        protected override string TableName => "filas_processamento";
        protected override string IdColumn => "id";

        protected override FilaProcessamento MapFromDataRow(DataRow row)
        {
            return new FilaProcessamento
            {
                Id = Convert.ToInt32(row["id"]),
                NomeFila = row["nomefila"].ToString() ?? string.Empty,
                CodAtiv = row["codativ"].ToString() ?? string.Empty,
                TipoOperacao = Convert.ToInt32(row["tipooperacao"]),
                StatusFila = Convert.ToInt32(row["statusfila"]),
                MensagemJson = row["mensagemjson"].ToString() ?? string.Empty,
                TentativasProcessamento = Convert.ToInt32(row["tentativasprocessamento"]),
                MaxTentativas = Convert.ToInt32(row["maxtentativas"]),
                ProximoProcessamento = row["proximoprocessamento"] == DBNull.Value
                    ? null : Convert.ToDateTime(row["proximoprocessamento"]),
                CorrelationId = row["correlationid"].ToString() ?? string.Empty,
                ErroProcessamento = row["erroprocessamento"].ToString(),
                Prioridade = Convert.ToInt32(row["prioridade"]),
                DataCriacao = Convert.ToDateTime(row["datacriacao"]),
                DataProcessamento = row["dataprocessamento"] == DBNull.Value
                    ? null : Convert.ToDateTime(row["dataprocessamento"])
            };
        }

        protected override MySqlParameter[] GetInsertParameters(FilaProcessamento entity)
        {
            return new[]
            {
                new MySqlParameter("@nomeFila", entity.NomeFila),
                new MySqlParameter("@codAtiv", entity.CodAtiv),
                new MySqlParameter("@tipoOperacao", entity.TipoOperacao),
                new MySqlParameter("@statusFila", entity.StatusFila),
                new MySqlParameter("@mensagemJson", entity.MensagemJson),
                new MySqlParameter("@tentativasProcessamento", entity.TentativasProcessamento),
                new MySqlParameter("@maxTentativas", entity.MaxTentativas),
                new MySqlParameter("@proximoProcessamento", entity.ProximoProcessamento ?? (object)DBNull.Value),
                new MySqlParameter("@correlationId", entity.CorrelationId),
                new MySqlParameter("@prioridade", entity.Prioridade),
                new MySqlParameter("@dataCriacao", entity.DataCriacao)
            };
        }

        protected override MySqlParameter[] GetUpdateParameters(FilaProcessamento entity)
        {
            return new[]
            {
                new MySqlParameter("@id", entity.Id),
                new MySqlParameter("@statusFila", entity.StatusFila),
                new MySqlParameter("@tentativasProcessamento", entity.TentativasProcessamento),
                new MySqlParameter("@proximoProcessamento", entity.ProximoProcessamento ?? (object)DBNull.Value),
                new MySqlParameter("@erroProcessamento", entity.ErroProcessamento ?? (object)DBNull.Value),
                new MySqlParameter("@dataProcessamento", entity.DataProcessamento ?? (object)DBNull.Value)
            };
        }

        protected override void SetEntityId(FilaProcessamento entity, int id)
        {
            entity.Id = id;
        }

        public async Task<IEnumerable<FilaProcessamento>> GetPendingItemsAsync(string nomeFila, int limit = 50)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Filas_processamento", "GetPendingItems");
                var parameters = new[]
                {
                    new MySqlParameter("@nomeFila", nomeFila),
                    new MySqlParameter("@limit", limit)
                };

                var dataTable = await _databaseService.ExecuteQueryAsync(sql, parameters);
                var result = new List<FilaProcessamento>();

                foreach (DataRow row in dataTable.Rows)
                {
                    result.Add(MapFromDataRow(row));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar itens pendentes da fila: {NomeFila}", nomeFila);
                throw;
            }
        }

        public async Task<IEnumerable<FilaProcessamento>> GetByPriorityAsync(string nomeFila, int limit = 50)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Filas_processamento", "GetByPriority");
                var parameters = new[]
                {
                    new MySqlParameter("@nomeFila", nomeFila),
                    new MySqlParameter("@limit", limit)
                };

                var dataTable = await _databaseService.ExecuteQueryAsync(sql, parameters);
                var result = new List<FilaProcessamento>();

                foreach (DataRow row in dataTable.Rows)
                {
                    result.Add(MapFromDataRow(row));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar itens por prioridade da fila: {NomeFila}", nomeFila);
                throw;
            }
        }

        public async Task<bool> UpdateStatusAsync(int id, int status, string? erro = null)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Filas_processamento", "UpdateStatus");
                var parameters = new[]
                {
                    new MySqlParameter("@id", id),
                    new MySqlParameter("@status", status),
                    new MySqlParameter("@erro", erro ?? (object)DBNull.Value),
                    new MySqlParameter("@dataProcessamento", DateTime.UtcNow)
                };

                var rowsAffected = await _databaseService.ExecuteNonQueryAsync(sql, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar status do item da fila: {Id}", id);
                throw;
            }
        }

        public async Task<bool> IncrementAttemptsAsync(int id, DateTime? nextProcessing = null)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Filas_processamento", "IncrementAttempts");
                var parameters = new[]
                {
                    new MySqlParameter("@id", id),
                    new MySqlParameter("@nextProcessing", nextProcessing ?? (object)DBNull.Value)
                };

                var rowsAffected = await _databaseService.ExecuteNonQueryAsync(sql, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao incrementar tentativas do item da fila: {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<FilaProcessamento>> GetFailedItemsAsync(string nomeFila, int limit = 50)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Filas_processamento", "GetFailedItems");
                var parameters = new[]
                {
                    new MySqlParameter("@nomeFila", nomeFila),
                    new MySqlParameter("@limit", limit)
                };

                var dataTable = await _databaseService.ExecuteQueryAsync(sql, parameters);
                var result = new List<FilaProcessamento>();

                foreach (DataRow row in dataTable.Rows)
                {
                    result.Add(MapFromDataRow(row));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar itens com falha da fila: {NomeFila}", nomeFila);
                throw;
            }
        }

        public async Task<bool> RequeueItemAsync(int id)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Filas_processamento", "RequeueItem");
                var parameters = new[]
                {
                    new MySqlParameter("@id", id),
                    new MySqlParameter("@dataProcessamento", DBNull.Value)
                };

                var rowsAffected = await _databaseService.ExecuteNonQueryAsync(sql, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao reprocessar item da fila: {Id}", id);
                throw;
            }
        }

        public async Task<int> GetQueueCountAsync(string nomeFila, int? status = null)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Filas_processamento", "GetQueueCount");
                var parameters = new[]
                {
                    new MySqlParameter("@nomeFila", nomeFila),
                    new MySqlParameter("@status", status?.ToString() ?? (object)DBNull.Value)
                };

                var count = await _databaseService.ExecuteScalarAsync(sql, parameters);
                return Convert.ToInt32(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao contar itens da fila: {NomeFila}", nomeFila);
                throw;
            }
        }

        public async Task<bool> CleanupProcessedItemsAsync(int olderThanDays = 7)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Filas_processamento", "CleanupProcessedItems");
                var parameters = new[] { new MySqlParameter("@olderThanDays", olderThanDays) };

                var rowsAffected = await _databaseService.ExecuteNonQueryAsync(sql, parameters);

                _logger.LogInformation("Limpeza de itens processados concluída. Registros removidos: {Count}", rowsAffected);

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao limpar itens processados das filas");
                throw;
            }
        }

        public async Task<Dictionary<string, int>> GetQueueStatisticsAsync()
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Filas_processamento", "GetQueueStatistics");
                var dataTable = await _databaseService.ExecuteQueryAsync(sql);

                var statistics = new Dictionary<string, int>();
                foreach (DataRow row in dataTable.Rows)
                {
                    var fila = row["nome_fila"].ToString() ?? "unknown";
                    var count = Convert.ToInt32(row["total"]);
                    statistics[fila] = count;
                }

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar estatísticas das filas");
                throw;
            }
        }

        public async Task<bool> CancelItemAsync(int id, string motivo)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Filas_processamento", "CancelItem");
                var parameters = new[]
                {
                    new MySqlParameter("@id", id),
                    new MySqlParameter("@motivo", motivo),
                    new MySqlParameter("@dataProcessamento", DateTime.UtcNow)
                };

                var rowsAffected = await _databaseService.ExecuteNonQueryAsync(sql, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao cancelar item da fila: {Id}", id);
                throw;
            }
        }
    }
}