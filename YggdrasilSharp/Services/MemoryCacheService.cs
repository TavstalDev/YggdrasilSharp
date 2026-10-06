using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Memory;

namespace Tavstal.YggdrasilSharp.Services;

/// <summary>
/// Service for managing memory cache operations.
/// </summary>
public class MemoryCacheService
{
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryCacheService"/> class.
    /// </summary>
    /// <param name="cache">The memory cache instance.</param>
    public MemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// Tries to retrieve a value from the cache.
    /// </summary>
    /// <typeparam name="T">The type of the value to retrieve.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The retrieved value, or default if the key does not exist.</param>
    /// <returns><c>true</c> if the value was found in the cache; otherwise, <c>false</c>.</returns>
    public bool TryGetValue<T>(string key, out T? value)
    {
        return _cache.TryGetValue(key, out value);
    }

    /// <summary>
    /// Sets a value in the cache with an optional expiration time.
    /// </summary>
    /// <typeparam name="T">The type of the value to store.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="absoluteExpirationRelativeToNow">The optional expiration time relative to now.</param>
    /// <param name="slidingExpiration">The optional expiration time compared to the last access time.</param>
    public void SetValue<T>(string key, T value, TimeSpan? absoluteExpirationRelativeToNow = null, TimeSpan? slidingExpiration = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        var options = new MemoryCacheEntryOptions();
        if (absoluteExpirationRelativeToNow.HasValue)
            options.SetAbsoluteExpiration(absoluteExpirationRelativeToNow.Value);
        if (slidingExpiration.HasValue)
            options.SetSlidingExpiration(slidingExpiration.Value);
        options.SetSize(1);
        _cache.Set(key, value, options);
    }

    /// <summary>
    /// Removes a value from the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    public void RemoveValue(string key)
    {
        _cache.Remove(key);
    }
}
