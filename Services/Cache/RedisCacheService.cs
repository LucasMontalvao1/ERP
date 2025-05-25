using API.Services.Cache.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace API.Services.Cache
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDatabase _database;
        private readonly IServer _server;
        private readonly ILogger<RedisCacheService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
        {
            _database = redis.GetDatabase();
            _server = redis.GetServer(redis.GetEndPoints().First());
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            try
            {
                var value = await _database.StringGetAsync(key);
                if (!value.HasValue)
                    return null;

                return JsonSerializer.Deserialize<T>(value, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter cache para a chave: {Key}", key);
                return null;
            }
        }

        public async Task<string?> GetAsync(string key)
        {
            try
            {
                return await _database.StringGetAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter cache para a chave: {Key}", key);
                return null;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            try
            {
                var json = JsonSerializer.Serialize(value, _jsonOptions);
                await _database.StringSetAsync(key, json, expiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao definir cache para a chave: {Key}", key);
            }
        }

        public async Task SetAsync(string key, string value, TimeSpan? expiration = null)
        {
            try
            {
                await _database.StringSetAsync(key, value, expiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao definir cache para a chave: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _database.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao remover cache para a chave: {Key}", key);
            }
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            try
            {
                var keys = _server.Keys(pattern: pattern);
                var keyArray = keys.ToArray();
                if (keyArray.Length > 0)
                {
                    await _database.KeyDeleteAsync(keyArray);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao remover cache por padrão: {Pattern}", pattern);
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                return await _database.KeyExistsAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar existência da chave: {Key}", key);
                return false;
            }
        }

        public async Task<long> IncrementAsync(string key, long value = 1)
        {
            try
            {
                return await _database.StringIncrementAsync(key, value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao incrementar chave: {Key}", key);
                return 0;
            }
        }

        public async Task<long> DecrementAsync(string key, long value = 1)
        {
            try
            {
                return await _database.StringDecrementAsync(key, value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao decrementar chave: {Key}", key);
                return 0;
            }
        }
    }
}
