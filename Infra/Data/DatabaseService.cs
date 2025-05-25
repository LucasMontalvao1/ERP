using MySqlConnector;
using System.Data;

namespace API.Infra.Data
{
    public interface IDatabaseService
    {
        Task<DataTable> ExecuteQueryAsync(string query, MySqlParameter[] parameters = null);
        Task<int> ExecuteNonQueryAsync(string query, MySqlParameter[] parameters = null);
        Task<object> ExecuteScalarAsync(string query, MySqlParameter[] parameters = null);
        MySqlConnection CreateConnection();
    }

    public class MySqlDatabaseService : IDatabaseService, IDisposable
    {
        private readonly string _connectionString;
        private readonly ILogger<MySqlDatabaseService> _logger;
        private MySqlConnection _connection;
        private bool _disposed = false;

        public MySqlDatabaseService(IConfiguration configuration, ILogger<MySqlDatabaseService> logger)
        {
            _connectionString = configuration.GetConnectionString("ConfiguracaoPadrao")
                ?? throw new ArgumentNullException(nameof(configuration),
                    "A string de conexão 'ConfiguracaoPadrao' não foi encontrada");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public MySqlConnection CreateConnection()
        {
            try
            {
                _connection = new MySqlConnection(_connectionString);
                return _connection;
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "Erro ao criar conexão com o banco de dados");
                throw new Exception("Erro ao criar conexão com o banco de dados.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Um erro inesperado ocorreu ao criar a conexão com o banco de dados");
                throw new Exception("Um erro inesperado ocorreu ao criar a conexão.", ex);
            }
        }

        private async Task<MySqlConnection> GetOpenConnectionAsync()
        {
            var connection = CreateConnection();
            await connection.OpenAsync();
            return connection;
        }

        public async Task<DataTable> ExecuteQueryAsync(string query, MySqlParameter[] parameters = null)
        {
            _logger.LogDebug($"Executando query: {query}");

            try
            {
                using var connection = await GetOpenConnectionAsync();
                using var command = new MySqlCommand(query, connection);

                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }

                var dataTable = new DataTable();
                using var dataAdapter = new MySqlDataAdapter(command);
                dataAdapter.Fill(dataTable);

                _logger.LogDebug($"Query executada com sucesso. Linhas retornadas: {dataTable.Rows.Count}");
                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao executar query: {query}");
                throw new Exception("Erro ao executar consulta no banco de dados.", ex);
            }
        }

        public async Task<int> ExecuteNonQueryAsync(string query, MySqlParameter[] parameters = null)
        {
            _logger.LogDebug($"Executando non-query: {query}");

            try
            {
                using var connection = await GetOpenConnectionAsync();
                using var command = new MySqlCommand(query, connection);

                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }

                var result = await command.ExecuteNonQueryAsync();
                _logger.LogDebug($"Non-query executada com sucesso. Linhas afetadas: {result}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao executar non-query: {query}");
                throw new Exception("Erro ao executar comando no banco de dados.", ex);
            }
        }

        public async Task<object> ExecuteScalarAsync(string query, MySqlParameter[] parameters = null)
        {
            _logger.LogDebug($"Executando scalar: {query}");

            try
            {
                using var connection = await GetOpenConnectionAsync();
                using var command = new MySqlCommand(query, connection);

                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }

                var result = await command.ExecuteScalarAsync();
                _logger.LogDebug($"Scalar executado com sucesso. Resultado: {result}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao executar scalar: {query}");
                throw new Exception("Erro ao executar consulta escalar no banco de dados.", ex);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing && _connection != null)
                {
                    _connection.Dispose();
                }

                _disposed = true;
            }
        }

        ~MySqlDatabaseService()
        {
            Dispose(false);
        }
    }
}
