using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;

// Sample:
// ObjectCacheHelper.Get<model>(key);
// ObjectCacheHelper.Add(model, key, DateTime.Now.AddMinutes(10));

namespace Common
{
    public class ObjectCacheHelper
    {
        private static readonly ObjectCache _cache = MemoryCache.Default;

        public static T Get<T>(string key) where T : class
        {
            try
            {
                return (T)_cache[key];
            }
            catch
            {
                return null;
            }
        }

        public static void Add<T>(T objectToCache, string key, DateTime expireTime) where T : class
        {
            _cache.Add(key, objectToCache, expireTime);
        }

        public static void Add(object objectToCache, string key, DateTime expireTime)
        {
            _cache.Add(key, objectToCache, expireTime);
        }

        public static void Remove(string key)
        {
            _cache.Remove(key);
        }

        public static bool Exists(string key)
        {
            return _cache.Get(key) != null;
        }

        public static List<string> GetAll()
        {
            return _cache.Select(keyValuePair => keyValuePair.Key).ToList();
        }
    }
}
