using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace TheEliteExplorer.Infrastructure
{
    internal static class CacheExtensions
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new ConcurrentDictionary<string, SemaphoreSlim>();

        internal static async Task<T> GetOrSetFromCacheAsync<T>(
            this IDistributedCache cache,
            string cacheKey,
            DistributedCacheEntryOptions options,
            Func<Task<T>> getWithoutCacheDelegate)
            where T : class
        {
            T datas = await cache.GetFromCacheAsync<T>(cacheKey).ConfigureAwait(false);
            if (datas == null)
            {
                SemaphoreSlim semaphore = _semaphores.GetOrAdd(cacheKey, new SemaphoreSlim(1, 1));
                semaphore.Wait();
                try
                {
                    datas = await cache.GetFromCacheAsync<T>(cacheKey).ConfigureAwait(false);
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

        private static async Task<T> GetFromCacheAsync<T>(this IDistributedCache cache, string cacheKey)
        {
            var bytesFromCache = await cache.GetAsync(cacheKey).ConfigureAwait(false);
            if (bytesFromCache != null)
            {
                return Deserialize<T>(bytesFromCache);
            }

            return default(T);
        }

        private static T Deserialize<T>(byte[] bytesFromCache)
        {
            var bf = new BinaryFormatter();
            using (var ms = new MemoryStream(bytesFromCache))
            {
                return (T)bf.Deserialize(ms);
            }
        }

        private static byte[] Serialize<T>(T datas)
        {
            var bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, datas);
                return ms.ToArray();
            }
        }
    }
}
