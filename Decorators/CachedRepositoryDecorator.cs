using API.Services.Cache.Interfaces;

namespace API.Decorators
{
    public abstract class CachedRepositoryDecorator<T>
    {
        protected readonly ICacheService _cacheService;
        protected readonly ILogger _logger;
        protected virtual TimeSpan DefaultCacheExpiration => TimeSpan.FromMinutes(30);

        protected CachedRepositoryDecorator(ICacheService cacheService, ILogger logger)
        {
            _cacheService = cacheService;
            _logger = logger;
        }

        protected async Task<TResult?> GetFromCacheOrExecuteAsync<TResult>(
            string cacheKey,
            Func<Task<TResult>> factory,
            TimeSpan? expiration = null) where TResult : class
        {
            try
            {
                // Tentar buscar do cache
                var cached = await _cacheService.GetAsync<TResult>(cacheKey);
                if (cached != null)
                {
                    _logger.LogDebug("Cache hit para a chave: {CacheKey}", cacheKey);
                    return cached;
                }

                // Se não encontrou no cache, executar a função
                _logger.LogDebug("Cache miss para a chave: {CacheKey}", cacheKey);
                var result = await factory();

                if (result != null)
                {
                    await _cacheService.SetAsync(cacheKey, result, expiration ?? DefaultCacheExpiration);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar cache para a chave: {CacheKey}", cacheKey);
                return await factory();
            }
        }

        protected async Task InvalidateCacheAsync(string pattern)
        {
            try
            {
                await _cacheService.RemoveByPatternAsync(pattern);
                _logger.LogDebug("Cache invalidado para o padrão: {Pattern}", pattern);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao invalidar cache para o padrão: {Pattern}", pattern);
            }
        }
    }
}