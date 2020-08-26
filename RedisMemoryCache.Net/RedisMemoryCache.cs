using System;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace RedisMemoryCache.Net
{
    public class RedisMemoryCache : IDisposable
    {
        private readonly ConnectionMultiplexer _redis;
        private readonly TimedMemoryCache.Net.TimedMemoryCache _cache;
        private readonly IDatabase _database;
        private readonly bool _synchronize;
        private readonly TimeSpan? _expires;

        public event TimeoutCallback OnTimeout;

        public RedisMemoryCache(string connectionString, int seconds = 300, TimeSpan? expires = null, bool synchronize = true)
        {
            _redis = ConnectionMultiplexer.Connect(connectionString);
            _database = _redis.GetDatabase();

            _cache = new TimedMemoryCache.Net.TimedMemoryCache(seconds);
            _cache.OnTimeout += Cache_OnTimeout;
            _expires = expires;
            _synchronize = synchronize;
        }

        public dynamic this[string key]
        {
            get => _cache[key];
            set
            {
                _cache[key] = value;
                if (!_synchronize)
                    return;

                var serializable = ((Type)value.GetType()).IsSerializable;
                if (!serializable) 
                    return;

                var json = JsonConvert.SerializeObject(value);
                _database.StringSet(key, json, _expires, When.Always, CommandFlags.FireAndForget);
            }
        }

        public void Write(string key, dynamic value, TimeSpan? expires = null, bool synchronize = true)
        {
            _cache.Write(key, value);
            if (!_synchronize)
                return;

            var serializable = ((Type)value.GetType()).IsSerializable;
            if (!serializable) 
                return;

            var json = JsonConvert.SerializeObject(value);
            _database.StringSet(key, json, expires, When.Always, CommandFlags.FireAndForget);
        }

        public void Write(string key, dynamic value, long seconds, TimeSpan? expires = null, bool synchronize = true)
        {
            _cache.Write(key, value, seconds);
            if (!_synchronize)
                return;

            var serializable = ((Type)value.GetType()).IsSerializable;
            if (!serializable) 
                return;

            var json = JsonConvert.SerializeObject(value);
            _database.StringSet(key, json, expires, When.Always, CommandFlags.FireAndForget);
        }

        public T Write<T>(string key, T value, TimeSpan? expires = null, bool synchronize = true)
        {
            var entry = _cache.Write<T>(key, value);
            if (!_synchronize)
                return entry;

            var serializable = value.GetType().IsSerializable;
            if (!serializable) 
                return entry;

            var json = JsonConvert.SerializeObject(value);
            _database.StringSet(key, json, expires, When.Always, CommandFlags.FireAndForget);

            return entry;
        }

        public T Write<T>(string key, T value, long seconds, TimeSpan? expires = null, bool synchronize = true)
        {
            var entry = _cache.Write<T>(key, value, seconds);
            if (!_synchronize)
                return entry;

            var serializable = value.GetType().IsSerializable;
            if (!serializable) 
                return entry;

            var json = JsonConvert.SerializeObject(value);
            _database.StringSet(key, json, expires, When.Always, CommandFlags.FireAndForget);

            return entry;
        }

        public T Read<T>(string key)
        {
            return _cache.Read<T>(key);
        }

        public void Delete(string key, bool synchronize = true)
        {
            _cache.Delete(key);

            if (synchronize)
                _database.KeyDelete(key, CommandFlags.FireAndForget);
        }

        public T Delete<T>(string key, bool synchronize = true)
        {
            var entry = _cache.Delete<T>(key);

            if (synchronize)
                _database.KeyDelete(key, CommandFlags.FireAndForget);

            return entry;
        }

        public void Dispose()
        {
            _redis?.Dispose();
            _cache?.Dispose();
        }

        private void Cache_OnTimeout(TimedMemoryCache.Net.TimedMemoryCache source, string key, dynamic value, long timeout)
        {
            try
            {
                var json = _database.StringGet(key);
                if (!json.HasValue)
                {
                    OnTimeout?.Invoke(source, key, value, timeout, false, true);
                    return;
                }

                var redisValue = JsonConvert.DeserializeObject<dynamic>(json.ToString());
                source.Write(key, redisValue, timeout);

                OnTimeout?.Invoke(source, key, redisValue, timeout, false, false);
            }
            catch (RedisTimeoutException)
            {
                // If we can't reach Redis, reuse what's in memory for now until we can try again in 3 seconds.
                source.Write(key, value, 3);
                OnTimeout?.Invoke(source, key, value, 3, true, false);
            }
        }
    }
}
