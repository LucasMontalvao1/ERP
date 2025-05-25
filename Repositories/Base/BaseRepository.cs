using API.Infra.Data;
using API.Repositories.Base.Interfaces;
using MySqlConnector;
using System.Data;

namespace API.Repositories.Base
{
    public abstract class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        protected readonly IDatabaseService _databaseService;
        protected readonly ILogger<BaseRepository<T>> _logger;

        protected BaseRepository(IDatabaseService databaseService, ILogger<BaseRepository<T>> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        // Métodos abstratos que cada repositório deve implementar
        protected abstract string TableName { get; }
        protected abstract string IdColumn { get; }
        protected abstract T MapFromDataRow(DataRow row);
        protected abstract MySqlParameter[] GetInsertParameters(T entity);
        protected abstract MySqlParameter[] GetUpdateParameters(T entity);
        protected abstract void SetEntityId(T entity, int id);

        public virtual async Task<T?> GetByIdAsync(int id)
        {
            try
            {
                var query = $"SELECT * FROM {TableName} WHERE {IdColumn} = @id";
                var parameters = new[] { new MySqlParameter("@id", id) };

                var dataTable = await _databaseService.ExecuteQueryAsync(query, parameters);

                if (dataTable.Rows.Count == 0)
                    return null;

                return MapFromDataRow(dataTable.Rows[0]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar {Entity} por ID: {Id}", typeof(T).Name, id);
                throw;
            }
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            try
            {
                var query = $"SELECT * FROM {TableName}";
                var dataTable = await _databaseService.ExecuteQueryAsync(query);

                var result = new List<T>();
                foreach (DataRow row in dataTable.Rows)
                {
                    result.Add(MapFromDataRow(row));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar todos os {Entity}", typeof(T).Name);
                throw;
            }
        }

        public virtual async Task<IEnumerable<T>> GetPagedAsync(int page, int pageSize)
        {
            try
            {
                var offset = (page - 1) * pageSize;
                var query = $"SELECT * FROM {TableName} LIMIT @pageSize OFFSET @offset";
                var parameters = new[]
                {
                    new MySqlParameter("@pageSize", pageSize),
                    new MySqlParameter("@offset", offset)
                };

                var dataTable = await _databaseService.ExecuteQueryAsync(query, parameters);

                var result = new List<T>();
                foreach (DataRow row in dataTable.Rows)
                {
                    result.Add(MapFromDataRow(row));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar {Entity} paginado. Página: {Page}, Tamanho: {PageSize}",
                    typeof(T).Name, page, pageSize);
                throw;
            }
        }

        public virtual async Task<T> CreateAsync(T entity)
        {
            try
            {
                var parameters = GetInsertParameters(entity);
                var columns = string.Join(", ", parameters.Select(p => p.ParameterName.TrimStart('@')));
                var values = string.Join(", ", parameters.Select(p => p.ParameterName));

                var query = $"INSERT INTO {TableName} ({columns}) VALUES ({values}); SELECT LAST_INSERT_ID();";

                var newId = await _databaseService.ExecuteScalarAsync(query, parameters);
                SetEntityId(entity, Convert.ToInt32(newId));

                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar {Entity}", typeof(T).Name);
                throw;
            }
        }

        public virtual async Task<T> UpdateAsync(T entity)
        {
            try
            {
                var parameters = GetUpdateParameters(entity);
                var setClause = string.Join(", ",
                    parameters.Where(p => p.ParameterName != "@id")
                             .Select(p => $"{p.ParameterName.TrimStart('@')} = {p.ParameterName}"));

                var query = $"UPDATE {TableName} SET {setClause} WHERE {IdColumn} = @id";

                await _databaseService.ExecuteNonQueryAsync(query, parameters);
                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar {Entity}", typeof(T).Name);
                throw;
            }
        }

        public virtual async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var query = $"DELETE FROM {TableName} WHERE {IdColumn} = @id";
                var parameters = new[] { new MySqlParameter("@id", id) };

                var rowsAffected = await _databaseService.ExecuteNonQueryAsync(query, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar {Entity} com ID: {Id}", typeof(T).Name, id);
                throw;
            }
        }

        public virtual async Task<bool> ExistsAsync(int id)
        {
            try
            {
                var query = $"SELECT COUNT(1) FROM {TableName} WHERE {IdColumn} = @id";
                var parameters = new[] { new MySqlParameter("@id", id) };

                var count = await _databaseService.ExecuteScalarAsync(query, parameters);
                return Convert.ToInt32(count) > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar existência de {Entity} com ID: {Id}", typeof(T).Name, id);
                throw;
            }
        }

        public virtual async Task<int> CountAsync()
        {
            try
            {
                var query = $"SELECT COUNT(*) FROM {TableName}";
                var count = await _databaseService.ExecuteScalarAsync(query);
                return Convert.ToInt32(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao contar {Entity}", typeof(T).Name);
                throw;
            }
        }
    }
}