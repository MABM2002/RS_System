using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Rs_system.Services;

public interface IQueryCacheService
{
    Task<T?> GetOrCreateAsync<T>(string cacheKey, Func<Task<T>> factory, TimeSpan? expiration = null);
    void Remove(string cacheKey);
    void Clear();
}

public class QueryCacheService : IQueryCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<QueryCacheService> _logger;
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);

    public QueryCacheService(IMemoryCache cache, ILogger<QueryCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetOrCreateAsync<T>(string cacheKey, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        if (_cache.TryGetValue(cacheKey, out T? cachedValue))
        {
            _logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
            return cachedValue;
        }

        _logger.LogDebug("Cache miss for key: {CacheKey}", cacheKey);
        var value = await factory();
        
        if (value != null)
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? DefaultExpiration,
                Size = 1 // Each cache entry has size 1 for memory management
            };
            
            _cache.Set(cacheKey, value, cacheOptions);
        }

        return value;
    }

    public void Remove(string cacheKey)
    {
        _cache.Remove(cacheKey);
        _logger.LogDebug("Cache removed for key: {CacheKey}", cacheKey);
    }

    public void Clear()
    {
        if (_cache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0); // Clear all cache entries
            _logger.LogInformation("Cache cleared");
        }
    }
}