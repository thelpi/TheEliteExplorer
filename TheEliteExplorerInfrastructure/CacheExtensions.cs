using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace TheEliteExplorerInfrastructure
{
    /// <summary>
    /// Cache extension methods.
    /// </summary>
    internal static class CacheExtensions
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new ConcurrentDictionary<string, SemaphoreSlim>();

        /// <summary>
        /// Gets a data from cache if possible, otherwise from original source then sets cache.
        /// </summary>
        /// <typeparam name="T">Targeted data type.</typeparam>
        /// <param name="cache">Cache provider.</param>
        /// <param name="cacheKey">Cache key.</param>
        /// <param name="options">Cache options.</param>
        /// <param name="getWithoutCacheDelegate">Delegate to retrieve data from the original source.</param>
        /// <returns>The data.</returns>
        internal static async Task<T> GetOrSetFromCache<T>(
            this IDistributedCache cache,
            string cacheKey,
            DistributedCacheEntryOptions options,
            Func<Task<T>> getWithoutCacheDelegate)
            where T : class, new()
        {
            T datas = await cache.GetFromCache<T>(cacheKey).ConfigureAwait(false);
            if (datas == null)
            {
                SemaphoreSlim semaphore = _semaphores.GetOrAdd(cacheKey, new SemaphoreSlim(1, 1));
                await semaphore.WaitAsync().ConfigureAwait(false);
                try
                {
                    datas = await cache.GetFromCache<T>(cacheKey).ConfigureAwait(false);
                    if (datas == null)
                    {
                        datas = await getWithoutCacheDelegate().ConfigureAwait(false);
                        if (datas != null)
                        {
                            await cache.SetAsync(cacheKey, Serialize(datas), options).ConfigureAwait(false);
                        }
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }

            return datas;
        }

        private static async Task<T> GetFromCache<T>(this IDistributedCache cache, string cacheKey) where T : new()
        {
            var bytesFromCache = await cache.GetAsync(cacheKey).ConfigureAwait(false);
            if (bytesFromCache != null && bytesFromCache.Length > 0)
            {
                return Deserialize<T>(bytesFromCache);
            }

            return default(T);
        }

        private static T Deserialize<T>(byte[] bytesFromCache) where T : new()
        {
            return JsonConvert.DeserializeObject<T>(Encoding.Default.GetString(bytesFromCache));
        }

        private static byte[] Serialize<T>(T datas) where T : new()
        {
            return Encoding.Default.GetBytes(JsonConvert.SerializeObject(datas));
        }
    }
}
