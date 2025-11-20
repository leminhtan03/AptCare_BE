using AptCare.Service.Services.Interfaces;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Implements
{
    public class RedisCacheService : IRedisCacheService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;

        public RedisCacheService(IConnectionMultiplexer redis)
        {
            _redis = redis;
            _db = _redis.GetDatabase();
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var value = await _db.StringGetAsync(key);
            if (value.IsNullOrEmpty) return default;
            return JsonSerializer.Deserialize<T>(value);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
        {
            var jsonData = JsonSerializer.Serialize(value);
            await _db.StringSetAsync(key, jsonData, expiration);
        }

        public async Task RemoveAsync(string key)
        {
            await _db.KeyDeleteAsync(key);
        }

        public async Task<IEnumerable<string>> GetKeysAsync(string pattern)
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            return server.Keys(pattern: pattern).Select(k => k.ToString());
        }
        public async Task RemoveByPrefixAsync(string prefix)
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: $"{prefix}*").ToArray(); 

            if (keys.Length > 0)
            {
                await _db.KeyDeleteAsync(keys); 
            }
        }

        public async Task ClearAllCacheAsync()
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys().ToArray(); 

            if (keys.Length > 0)
            {
                await _db.KeyDeleteAsync(keys); 
            }
        }
    }
}
