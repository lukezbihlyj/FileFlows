using System.Collections.Concurrent;
using System.Threading;

namespace FileFlows.Client.Services;

/// <summary>
/// Provides caching functionality for storing and retrieving data with expiration logic and thread-safety.
/// </summary>
public class CacheService
{
    /// <summary>
    /// A thread-safe dictionary to store cached items along with their expiration time.
    /// </summary>
    private ConcurrentDictionary<string, CacheEntry<object>> _cache = new();

    /// <summary>
    /// A dictionary to store semaphores for each cache key to ensure thread-safety when multiple threads request the same data.
    /// </summary>
    private ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    /// <summary>
    /// Retrieves data from the cache if it exists and is not expired, 
    /// otherwise calls the provided method to fetch the data, stores it in the cache, and returns it.
    /// Ensures only one thread loads the data at a time for the same cache key.
    /// </summary>
    /// <typeparam name="T">The type of data to retrieve or fetch.</typeparam>
    /// <param name="key">The key to identify the cached data.</param>
    /// <param name="expiryInSeconds">The number of seconds after which the cache entry expires.</param>
    /// <param name="getDataFunc">A function to fetch the data if the cache is empty or expired.</param>
    /// <returns>
    /// The data either from the cache if available, or from the provided method if the cache entry is expired or does not exist.
    /// </returns>
    public async Task<T> GetFromCache<T>(string key, int expiryInSeconds, Func<Task<T>> getDataFunc)
    {
        // Check if the cache contains the key and if the entry has not expired
        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry != null && entry.Expiry > DateTime.UtcNow)
            {
                return (T)entry.Data; // Return cached data
            }
        }

        // Ensure that only one thread can load the data for this key
        var semaphore = _locks.GetOrAdd(key, new SemaphoreSlim(1, 1));

        try
        {
            await semaphore.WaitAsync();

            // After acquiring the semaphore, check the cache again (in case another thread already loaded it)
            if (_cache.TryGetValue(key, out entry))
            {
                if (entry.Expiry > DateTime.UtcNow)
                {
                    return (T)entry.Data; // Return cached data
                }
            }

            // Cache miss or the entry has expired, so fetch new data
            T data = await getDataFunc();

            // Calculate new expiry time
            var expiry = DateTime.UtcNow.AddSeconds(expiryInSeconds);

            // Store the data in the cache with the updated expiry time
            _cache[key] = new CacheEntry<object>(data, expiry);

            return data;
        }
        finally
        {
            semaphore.Release(); // Ensure that the semaphore is released even if an exception occurs

            // Cleanup: Remove the semaphore if no one else is waiting for it
            _locks.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// Clears all cached data.
    /// </summary>
    /// <param name="key">the key to clear</param>
    public void Clear(string key)
    {
        // Remove the cache entry if it exists
        _cache.TryRemove(key, out _);

        // Remove the associated semaphore if it exists
        _locks.TryRemove(key, out _);
    }

    /// <summary>
    /// Clears all cached data.
    /// </summary>
    public void ClearAll()
    {
        // Clear all cache entries
        _cache.Clear();

        // Clear all semaphores
        _locks.Clear();
    }
}


/// <summary>
/// Represents a cache entry with data and an expiry time.
/// </summary>
/// <typeparam name="T">The type of the data being cached.</typeparam>
public class CacheEntry<T>
{
    /// <summary>
    /// Gets or sets the cached data.
    /// </summary>
    public T Data { get; set; }

    /// <summary>
    /// Gets or sets the expiration time of the cache entry.
    /// </summary>
    public DateTime Expiry { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheEntry{T}"/> class.
    /// </summary>
    /// <param name="data">The data to cache.</param>
    /// <param name="expiry">The expiration time for the cache entry.</param>
    public CacheEntry(T data, DateTime expiry)
    {
        Data = data;
        Expiry = expiry;
    }
}
