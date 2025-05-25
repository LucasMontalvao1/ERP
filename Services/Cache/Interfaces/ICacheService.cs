namespace API.Services.Cache.Interfaces
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key) where T : class;
        Task<string?> GetAsync(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
        Task SetAsync(string key, string value, TimeSpan? expiration = null);
        Task RemoveAsync(string key);
        Task RemoveByPatternAsync(string pattern);
        Task<bool> ExistsAsync(string key);
        Task<long> IncrementAsync(string key, long value = 1);
        Task<long> DecrementAsync(string key, long value = 1);
    }
}
