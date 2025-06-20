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
    public class ConfiguracaoIntegracaoRepository : BaseRepository<ConfiguracaoIntegracao>, IConfiguracaoIntegracaoRepository
    {
        private readonly SqlLoader _sqlLoader;

        public ConfiguracaoIntegracaoRepository(
            IDatabaseService databaseService,
            ILogger<ConfiguracaoIntegracaoRepository> logger,
            SqlLoader sqlLoader)
            : base(databaseService, logger)
        {
            _sqlLoader = sqlLoader;
        }

        protected override string TableName => "configuracoes_integracao";
        protected override string IdColumn => "id";

        protected override ConfiguracaoIntegracao MapFromDataRow(DataRow row)
        {
            return new ConfiguracaoIntegracao
            {
                Id = Convert.ToInt32(row["id"]),
                Nome = row["nome"].ToString() ?? string.Empty,
                Descricao = row["descricao"].ToString(),
                UrlApi = row["urlapi"].ToString() ?? string.Empty,
                Login = row["login"].ToString() ?? string.Empty,
                SenhaCriptografada = row["senhacriptografada"].ToString() ?? string.Empty,
                Endpoints = row["endpoints"].ToString(),
                VersaoApi = row["versaoapi"].ToString() ?? "v1",
                EndpointLogin = row["endpointlogin"].ToString() ?? "/login",
                EndpointPrincipal = row["endpointprincipal"].ToString() ?? "/api/v1",
                TokenAtual = row["tokenatual"].ToString(),
                TokenExpiracao = row["tokenexpiracao"] == DBNull.Value
                    ? null : Convert.ToDateTime(row["tokenexpiracao"]),
                Ativo = Convert.ToBoolean(row["ativo"]),
                TimeoutSegundos = Convert.ToInt32(row["timeoutsegundos"]),
                MaxTentativas = Convert.ToInt32(row["maxtentativas"]),
                HeadersCustomizados = row["headerscustomizados"].ToString(),
                ConfiguracaoPadrao = Convert.ToBoolean(row["configuracaopadrao"]),
                RetryPolicy = row["retrypolicy"].ToString() ?? "exponential",
                RetryDelayBaseSeconds = Convert.ToInt32(row["retrydelaybaseseconds"]),
                EnableCircuitBreaker = Convert.ToBoolean(row["enablecircuitbreaker"]),
                CircuitBreakerThreshold = Convert.ToInt32(row["circuitbreakerthreshold"]),
                DataCriacao = Convert.ToDateTime(row["datacriacao"]),
                DataAtualizacao = row["dataatualizacao"] == DBNull.Value
                    ? null : Convert.ToDateTime(row["dataatualizacao"]),
                CriadoPor = row["criadopor"] == DBNull.Value ? null : Convert.ToInt32(row["criadopor"]),
                AtualizadoPor = row["atualizadopor"] == DBNull.Value ? null : Convert.ToInt32(row["atualizadopor"]),
                Version = Convert.ToDateTime(row["version"])
            };
        }

        protected override MySqlParameter[] GetInsertParameters(ConfiguracaoIntegracao entity)
        {
            return new[]
            {
                new MySqlParameter("@nome", entity.Nome),
                new MySqlParameter("@descricao", entity.Descricao ?? (object)DBNull.Value),
                new MySqlParameter("@urlapi", entity.UrlApi), 
                new MySqlParameter("@login", entity.Login),
                new MySqlParameter("@senhacriptografada", entity.SenhaCriptografada), 
                new MySqlParameter("@versaoapi", entity.VersaoApi), 
                new MySqlParameter("@endpointlogin", entity.EndpointLogin),
                new MySqlParameter("@endpointprincipal", entity.EndpointPrincipal), 
                new MySqlParameter("@ativo", entity.Ativo),
                new MySqlParameter("@timeoutsegundos", entity.TimeoutSegundos), 
                new MySqlParameter("@maxtentativas", entity.MaxTentativas), 
                new MySqlParameter("@configuracaopadrao", entity.ConfiguracaoPadrao), 
                new MySqlParameter("@retrypolicy", entity.RetryPolicy), 
                new MySqlParameter("@retrydelay_base_seconds", entity.RetryDelayBaseSeconds), 
                new MySqlParameter("@enablecircuitbreaker", entity.EnableCircuitBreaker), 
                new MySqlParameter("@circuitbreakerthreshold", entity.CircuitBreakerThreshold),
                new MySqlParameter("@datacriacao", entity.DataCriacao), 
                new MySqlParameter("@criadopor", entity.CriadoPor ?? (object)DBNull.Value) 
            };
        }

        protected override MySqlParameter[] GetUpdateParameters(ConfiguracaoIntegracao entity)
        {
            return new[]
            {
                new MySqlParameter("@id", entity.Id),
                new MySqlParameter("@nome", entity.Nome),
                new MySqlParameter("@descricao", entity.Descricao ?? (object)DBNull.Value),
                new MySqlParameter("@urlapi", entity.UrlApi), 
                new MySqlParameter("@versaoapi", entity.VersaoApi),
                new MySqlParameter("@ativo", entity.Ativo),
                new MySqlParameter("@timeoutsegundos", entity.TimeoutSegundos), 
                new MySqlParameter("@maxtentativas", entity.MaxTentativas), 
                new MySqlParameter("@configuracaopadrao", entity.ConfiguracaoPadrao), 
                new MySqlParameter("@retrypolicy", entity.RetryPolicy), 
                new MySqlParameter("@retrydelaybaseseconds", entity.RetryDelayBaseSeconds),
                new MySqlParameter("@enablecircuitbreaker", entity.EnableCircuitBreaker), 
                new MySqlParameter("@circuitbreakerthreshold", entity.CircuitBreakerThreshold), 
                new MySqlParameter("@dataatualizacao", DateTime.UtcNow), 
                new MySqlParameter("@atualizadopor", entity.AtualizadoPor ?? (object)DBNull.Value) 
            };
        }

        protected override void SetEntityId(ConfiguracaoIntegracao entity, int id)
        {
            entity.Id = id;
        }

        // Implementações específicas
        public async Task<ConfiguracaoIntegracao?> GetDefaultConfigAsync()
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Configuracoes_integracao", "GetDefaultConfig");
                var dataTable = await _databaseService.ExecuteQueryAsync(sql);

                if (dataTable.Rows.Count == 0)
                    return null;

                return MapFromDataRow(dataTable.Rows[0]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar configuração padrão");
                throw;
            }
        }

        public async Task<ConfiguracaoIntegracao?> GetByNameAsync(string nome)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Configuracoes_integracao", "GetByName");
                var parameters = new[] { new MySqlParameter("@nome", nome) };

                var dataTable = await _databaseService.ExecuteQueryAsync(sql, parameters);

                if (dataTable.Rows.Count == 0)
                    return null;

                return MapFromDataRow(dataTable.Rows[0]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar configuração por nome: {Nome}", nome);
                throw;
            }
        }

        public async Task<IEnumerable<ConfiguracaoIntegracao>> GetActiveConfigsAsync()
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Configuracoes_integracao", "GetActiveConfigs");
                var dataTable = await _databaseService.ExecuteQueryAsync(sql);

                var result = new List<ConfiguracaoIntegracao>();
                foreach (DataRow row in dataTable.Rows)
                {
                    result.Add(MapFromDataRow(row));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar configurações ativas");
                throw;
            }
        }

        public async Task<bool> SetAsDefaultAsync(int id)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Configuracoes_integracao", "SetAsDefault");
                var parameters = new[] { new MySqlParameter("@id", id) };

                var rowsAffected = await _databaseService.ExecuteNonQueryAsync(sql, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao definir configuração como padrão: {Id}", id);
                throw;
            }
        }

        public async Task<bool> TestConnectionAsync(int id)
        {
            try
            {
                var config = await GetByIdAsync(id);
                return config != null && config.Ativo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao testar conexão da configuração: {Id}", id);
                return false;
            }
        }

        public async Task<bool> UpdateTokenAsync(int id, string token, DateTime expiration)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Configuracoes_integracao", "UpdateToken");
                var parameters = new[]
                {
                    new MySqlParameter("@id", id),
                    new MySqlParameter("@token", token),
                    new MySqlParameter("@expiracao", expiration)
                };

                var rowsAffected = await _databaseService.ExecuteNonQueryAsync(sql, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar token da configuração: {Id}", id);
                throw;
            }
        }

        public async Task<bool> UpdateLastConnectionAsync(int id)
        {
            try
            {
                var sql = _sqlLoader.GetQuery("Configuracoes_integracao", "UpdateLastConnection");
                var parameters = new[] { new MySqlParameter("@id", id) };

                var rowsAffected = await _databaseService.ExecuteNonQueryAsync(sql, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar última conexão da configuração: {Id}", id);
                throw;
            }
        }

        public async Task<PagedResponse<IntegrationConfigDto>> GetPagedConfigsAsync(int page, int pageSize, bool? activeOnly = null)
        {
            try
            {
                var (validPage, validPageSize) = ValidationHelper.ValidatePagination(page, pageSize);
                var offset = (validPage - 1) * validPageSize;

                var parameters = new[]
                {
                    new MySqlParameter("@ativo", activeOnly?.ToString() ?? (object)DBNull.Value),
                    new MySqlParameter("@pageSize", validPageSize),
                    new MySqlParameter("@offset", offset)
                };

                // Contar total
                var countSql = _sqlLoader.GetQuery("Configuracoes_integracao", "CountPaged");
                var totalCount = Convert.ToInt32(await _databaseService.ExecuteScalarAsync(countSql, parameters.Take(1).ToArray()));

                // Buscar dados
                var dataSql = _sqlLoader.GetQuery("Configuracoes_integracao", "GetPaged");
                var dataTable = await _databaseService.ExecuteQueryAsync(dataSql, parameters);

                var items = new List<IntegrationConfigDto>();
                foreach (DataRow row in dataTable.Rows)
                {
                    items.Add(new IntegrationConfigDto
                    {
                        Id = Convert.ToInt32(row["id"]),
                        Nome = row["nome"].ToString() ?? string.Empty,
                        UrlApi = row["urlapi"].ToString() ?? string.Empty,
                        VersaoApi = row["versaoapi"].ToString() ?? string.Empty,
                        Ativo = Convert.ToBoolean(row["ativo"]),
                        ConfiguracaoPadrao = Convert.ToBoolean(row["configuracaopadrao"]),
                        TimeoutSegundos = Convert.ToInt32(row["timeoutsegundos"]),
                        MaxTentativas = Convert.ToInt32(row["maxtentativas"]),
                        DataCriacao = Convert.ToDateTime(row["datacriacao"])
                    });
                }

                return PagedResponse<IntegrationConfigDto>.Create(
                    items,
                    validPage,
                    validPageSize,
                    totalCount,
                    "Configurações recuperadas com sucesso"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar configurações paginadas");

                return PagedResponse<IntegrationConfigDto>.Create(
                    new List<IntegrationConfigDto>(),
                    page,
                    pageSize,
                    0,
                    "Erro ao buscar configurações paginadas"
                );
            }
        }
    }
}