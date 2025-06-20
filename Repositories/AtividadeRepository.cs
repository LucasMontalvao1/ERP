using API.Infra.Data;
using API.Models.DTOs.Atividade;
using API.Models.Entities;
using API.Models.Responses;
using API.Repositories.Interfaces;
using API.Helpers;
using API.SQL;
using MySqlConnector;
using System.Data;

namespace API.Repositories
{
    public class AtividadeRepository : IAtividadeRepository
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<AtividadeRepository> _logger;
        private readonly SqlLoader _sqlLoader;

        public AtividadeRepository(
            IDatabaseService databaseService,
            ILogger<AtividadeRepository> logger,
            SqlLoader sqlLoader)
        {
            _databaseService = databaseService;
            _logger = logger;
            _sqlLoader = sqlLoader;
        }

        private Atividade MapFromDataRow(DataRow row)
        {
            return new Atividade
            {
                CodAtiv = row["codativ"].ToString() ?? string.Empty,
                Ramo = row["ramo"].ToString() ?? string.Empty,
                PercDesc = Convert.ToDecimal(row["percdesc"]),
                CalculaSt = row["calculast"].ToString() ?? "N",
                StatusSincronizacao = Convert.ToInt32(row["statussincronizacao"]),
                DataUltimaSincronizacao = row["dataultimasincronizacao"] == DBNull.Value
                    ? null : Convert.ToDateTime(row["dataultimasincronizacao"]),
                TentativasSincronizacao = Convert.ToInt32(row["tentativassincronizacao"]),
                UltimoErroSincronizacao = row["ultimoerrosincronizacao"].ToString(),
                DataCriacao = Convert.ToDateTime(row["datacriacao"]),
                DataAtualizacao = row["dataatualizacao"] == DBNull.Value
                    ? null : Convert.ToDateTime(row["dataatualizacao"]),
                CriadoPor = row["criadopor"] == DBNull.Value ? null : Convert.ToInt32(row["criadopor"]),
                AtualizadoPor = row["atualizadopor"] == DBNull.Value ? null : Convert.ToInt32(row["atualizadopor"]),
                Version = Convert.ToDateTime(row["version"])
            };
        }

        private AtividadeListDto MapToListDto(DataRow row)
        {
            return new AtividadeListDto
            {
                CodAtiv = row["codativ"].ToString() ?? string.Empty,
                Ramo = row["ramo"].ToString() ?? string.Empty,
                PercDesc = Convert.ToDecimal(row["percdesc"]),
                CalculaSt = row["calculast"].ToString() ?? "N",
                StatusSincronizacao = Convert.ToInt32(row["statussincronizacao"]),
                StatusSincronizacaoDescricao = row["statussincronizacaodescricao"].ToString() ?? string.Empty,
                DataUltimaSincronizacao = row["dataultimasincronizacao"] == DBNull.Value
                    ? null : Convert.ToDateTime(row["dataultimasincronizacao"]),
                DataCriacao = Convert.ToDateTime(row["datacriacao"])
            };
        }

        public async Task<Atividade?> GetByCodAtivAsync(string codAtiv)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Atividade", "GetById");
                var parameters = new[] { new MySqlParameter("@codAtiv", codAtiv) };

                var dataTable = await _databaseService.ExecuteQueryAsync(sql, parameters);

                if (dataTable.Rows.Count == 0)
                    return null;

                return MapFromDataRow(dataTable.Rows[0]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar atividade por código: {CodAtiv}", codAtiv);
                throw;
            }
        }

        public async Task<bool> ExistsByCodAtivAsync(string codAtiv)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Atividade", "ExistsByCodAtiv");
                var parameters = new[] { new MySqlParameter("@codAtiv", codAtiv) };

                var count = await _databaseService.ExecuteScalarAsync(sql, parameters);
                return Convert.ToInt32(count) > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar existência da atividade: {CodAtiv}", codAtiv);
                throw;
            }
        }

        public async Task<PagedResponse<AtividadeListDto>> GetPagedAsync(AtividadeFilterDto filter)
        {
            try
            {
                var (page, pageSize) = ValidationHelper.ValidatePagination(filter.Page, filter.PageSize);
                var offset = (page - 1) * pageSize;

                var parameters = new[]
                {
                    new MySqlParameter("@codAtiv", string.IsNullOrEmpty(filter.CodAtiv) ? DBNull.Value : filter.CodAtiv),
                    new MySqlParameter("@ramo", string.IsNullOrEmpty(filter.Ramo) ? DBNull.Value : filter.Ramo),
                    new MySqlParameter("@calculaSt", string.IsNullOrEmpty(filter.CalculaSt) ? DBNull.Value : filter.CalculaSt),
                    new MySqlParameter("@statusSincronizacao", filter.StatusSincronizacao?.ToString() ?? (object)DBNull.Value),
                    new MySqlParameter("@dataCriacaoInicio", filter.DataCriacaoInicio ?? (object)DBNull.Value),
                    new MySqlParameter("@dataCriacaoFim", filter.DataCriacaoFim ?? (object)DBNull.Value),
                    new MySqlParameter("@orderBy", filter.OrderBy ?? "DataCriacao"),
                    new MySqlParameter("@orderDirection", filter.OrderDirection ?? "DESC"),
                    new MySqlParameter("@pageSize", pageSize),
                    new MySqlParameter("@offset", offset)
                };

                // Contar total
                var countSql = _sqlLoader.GetQuery("Atividade", "CountPaged");
                var totalCount = Convert.ToInt32(await _databaseService.ExecuteScalarAsync(countSql, parameters.Take(6).ToArray()));

                // Buscar dados
                var dataSql = _sqlLoader.GetQuery("Atividade", "GetPaged");
                var dataTable = await _databaseService.ExecuteQueryAsync(dataSql, parameters);

                var items = new List<AtividadeListDto>();
                foreach (DataRow row in dataTable.Rows)
                {
                    items.Add(MapToListDto(row));
                }

                return PagedResponse<AtividadeListDto>.Create(
                    items,
                    page,
                    pageSize,
                    totalCount,
                    "Dados recuperados com sucesso"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar atividades paginadas");
                throw;
            }
        }

        public async Task<Atividade> CreateAsync(Atividade entity)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Atividade", "Create");
                var parameters = new[]
                {
                    new MySqlParameter("@codAtiv", entity.CodAtiv),
                    new MySqlParameter("@ramo", entity.Ramo),
                    new MySqlParameter("@percDesc", entity.PercDesc),
                    new MySqlParameter("@calculaSt", entity.CalculaSt),
                    new MySqlParameter("@statusSincronizacao", entity.StatusSincronizacao),
                    new MySqlParameter("@tentativasSincronizacao", entity.TentativasSincronizacao),
                    new MySqlParameter("@dataCriacao", entity.DataCriacao),
                    new MySqlParameter("@criadoPor", entity.CriadoPor ?? (object)DBNull.Value)
                };

                await _databaseService.ExecuteNonQueryAsync(sql, parameters);
                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar atividade: {CodAtiv}", entity.CodAtiv);
                throw;
            }
        }

        public async Task<Atividade> UpdateAsync(Atividade entity)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Atividade", "Update");
                var parameters = new[]
                {
                    new MySqlParameter("@codAtiv", entity.CodAtiv),
                    new MySqlParameter("@ramo", entity.Ramo),
                    new MySqlParameter("@percDesc", entity.PercDesc),
                    new MySqlParameter("@calculaSt", entity.CalculaSt),
                    new MySqlParameter("@statusSincronizacao", entity.StatusSincronizacao),
                    new MySqlParameter("@dataUltimaSincronizacao", entity.DataUltimaSincronizacao ?? (object)DBNull.Value),
                    new MySqlParameter("@tentativasSincronizacao", entity.TentativasSincronizacao),
                    new MySqlParameter("@ultimoErroSincronizacao", entity.UltimoErroSincronizacao ?? (object)DBNull.Value),
                    new MySqlParameter("@dataAtualizacao", DateTime.UtcNow),
                    new MySqlParameter("@atualizadoPor", entity.AtualizadoPor ?? (object)DBNull.Value)
                };

                await _databaseService.ExecuteNonQueryAsync(sql, parameters);
                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar atividade: {CodAtiv}", entity.CodAtiv);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string codAtiv)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Atividade", "Delete");
                var parameters = new[] { new MySqlParameter("@codAtiv", codAtiv) };

                var rowsAffected = await _databaseService.ExecuteNonQueryAsync(sql, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar atividade: {CodAtiv}", codAtiv);
                throw;
            }
        }

        public async Task<IEnumerable<Atividade>> GetPendingSyncAsync(int limit = 50)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Atividade", "GetPendingSync");
                var parameters = new[] { new MySqlParameter("@limit", limit) };

                var dataTable = await _databaseService.ExecuteQueryAsync(sql, parameters);
                var result = new List<Atividade>();

                foreach (DataRow row in dataTable.Rows)
                {
                    result.Add(MapFromDataRow(row));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar atividades pendentes de sincronização");
                throw;
            }
        }

        public async Task<IEnumerable<Atividade>> GetFailedSyncAsync(int limit = 50)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Atividade", "GetFailedSync");
                var parameters = new[] { new MySqlParameter("@limit", limit) };

                var dataTable = await _databaseService.ExecuteQueryAsync(sql, parameters);
                var result = new List<Atividade>();

                foreach (DataRow row in dataTable.Rows)
                {
                    result.Add(MapFromDataRow(row));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar atividades com falha de sincronização");
                throw;
            }
        }

        public async Task<bool> UpdateSyncStatusAsync(string codAtiv, int status, string? externalId = null, string? errorMessage = null)
        {
            try
            {
                var sql = status == 1
                    ? _sqlLoader.GetQuery("Atividade", "UpdateSyncStatusSuccess")
                    : _sqlLoader.GetQuery("Atividade", "UpdateSyncStatus");

                var parameters = status == 1
                    ? new[] { new MySqlParameter("@codAtiv", codAtiv) }
                    : new[]
                    {
                        new MySqlParameter("@codAtiv", codAtiv),
                        new MySqlParameter("@status", status),
                        new MySqlParameter("@dataUltimaSincronizacao", DateTime.UtcNow),
                        new MySqlParameter("@ultimoErroSincronizacao", errorMessage ?? (object)DBNull.Value)
                    };

                var rowsAffected = await _databaseService.ExecuteNonQueryAsync(sql, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar status de sincronização: {CodAtiv}", codAtiv);
                throw;
            }
        }

        public async Task<int> GetCountByStatusAsync(int status)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Atividade", "GetCountByStatus");
                var parameters = new[] { new MySqlParameter("@status", status) };

                var count = await _databaseService.ExecuteScalarAsync(sql, parameters);
                return Convert.ToInt32(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao contar atividades por status: {Status}", status);
                throw;
            }
        }

        public async Task<IEnumerable<Atividade>> SearchAsync(string searchTerm, int limit = 10)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Atividade", "Search");
                var parameters = new[]
                {
                    new MySqlParameter("@searchTerm", searchTerm),
                    new MySqlParameter("@limit", limit)
                };

                var dataTable = await _databaseService.ExecuteQueryAsync(sql, parameters);
                var result = new List<Atividade>();

                foreach (DataRow row in dataTable.Rows)
                {
                    result.Add(MapFromDataRow(row));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao pesquisar atividades: {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<bool> BulkUpdateSyncStatusAsync(List<string> codAtivs, int status)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Atividade", "BulkUpdateSyncStatus");
                var codAtivsString = string.Join("','", codAtivs);
                sql = sql.Replace("@codAtivs", $"'{codAtivsString}'");

                var parameters = new[] { new MySqlParameter("@status", status) };

                var rowsAffected = await _databaseService.ExecuteNonQueryAsync(sql, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar status em lote");
                throw;
            }
        }

        public async Task<IEnumerable<Atividade>> GetByRamoAsync(string ramo)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Atividade", "GetByRamo");
                var parameters = new[] { new MySqlParameter("@ramo", ramo) };

                var dataTable = await _databaseService.ExecuteQueryAsync(sql, parameters);
                var result = new List<Atividade>();

                foreach (DataRow row in dataTable.Rows)
                {
                    result.Add(MapFromDataRow(row));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar atividades por ramo: {Ramo}", ramo);
                throw;
            }
        }

        public async Task<bool> SoftDeleteAsync(string codAtiv, int userId)
        {
            return await DeleteAsync(codAtiv);
        }

        // Métodos não usados mas mantidos para compatibilidade com IBaseRepository
        public Task<Atividade?> GetByIdAsync(int id) => throw new NotImplementedException("Use GetByCodAtivAsync");
        public Task<IEnumerable<Atividade>> GetAllAsync() => throw new NotImplementedException("Use GetPagedAsync");
        public Task<IEnumerable<Atividade>> GetPagedAsync(int page, int pageSize) => throw new NotImplementedException("Use GetPagedAsync(filter)");
        public Task<bool> DeleteAsync(int id) => throw new NotImplementedException("Use DeleteAsync(codAtiv)");
        public Task<bool> ExistsAsync(int id) => throw new NotImplementedException("Use ExistsByCodAtivAsync");
        public Task<int> CountAsync() => throw new NotImplementedException("Use GetCountByStatusAsync");
    }
}