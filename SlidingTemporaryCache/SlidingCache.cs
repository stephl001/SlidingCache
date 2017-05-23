using System;
using System.Runtime.Caching;

namespace SlidingTemporaryCache
{
    /// <summary>
    /// A simple temporary sliding cache implementation.
    /// </summary>
    /// <remarks>
    /// This class uses lazy loading to avoid having to instanciate an instance prior
    /// to adding it to the cache.
    /// </remarks>
    public sealed class SlidingCache
    {
        private readonly MemoryCache _cache = MemoryCache.Default;
        private readonly string _keyPrefix = Guid.NewGuid().ToString("N");
        private readonly CacheItemPolicy _cachePolicy;

        /// <summary>
        /// Instanciate a new <see cref="SlidingCache"/> instance.
        /// </summary>
        /// <param name="slidingExpiration">The sliding expiration for this <see cref="SlidingCache"/> instance.</param>
        public SlidingCache(TimeSpan slidingExpiration)
        {
            _cachePolicy = new CacheItemPolicy();
            _cachePolicy.SlidingExpiration = slidingExpiration;
        }

        private string MakeKey(string key)
        {
            return string.Concat(_keyPrefix, "_", key);
        }

        /// <summary>
        /// Adds or gets a value from the internal cache.
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="key">The key used to fetched the value instanciated by the factory.</param>
        /// <param name="valueFactory">The factory that will create the value to be cached if allowed in the cache.</param>
        /// <returns>The already cached or newly added value.</returns>
        public T AddOrGetExisting<T>(string key, Func<T> valueFactory)
        {
            string realKey = MakeKey(key);

            var newValue = new Lazy<T>(valueFactory, true);
            var oldValue = _cache.AddOrGetExisting(realKey, newValue, _cachePolicy) as Lazy<T>;
            try
            {
                return (oldValue ?? newValue).Value;
            }
            catch (Exception e)
            {
                // If we get an initialization error while calling the facory handler,
                // we must make sure that the Lazy<T> instance is removed from the cache to
                // avoid potential memory leaks.
                _cache.Remove(realKey);
                throw new LazyInitializationException(e);
            }
        }
    }
}
