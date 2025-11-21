using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    public class RedisCacheController : BaseApiController
    {
        private readonly IRedisCacheService _redisCacheService;

        public RedisCacheController(IRedisCacheService redisCacheService)
        {
            _redisCacheService = redisCacheService;
        }

        // Clear all cache
        [HttpDelete("clear-all")]
        public async Task<IActionResult> ClearAllCache()
        {
            await _redisCacheService.ClearAllCacheAsync();
            return Ok("All cache cleared successfully.");
        }

        // Remove cache by a specific key
        [HttpDelete("remove/{key}")]
        public async Task<IActionResult> RemoveCache(string key)
        {
            await _redisCacheService.RemoveAsync(key);
            return Ok($"Cache with key '{key}' removed successfully.");
        }

        // Remove cache by prefix
        [HttpDelete("remove-by-prefix/{prefix}")]
        public async Task<IActionResult> RemoveCacheByPrefix(string prefix)
        {
            await _redisCacheService.RemoveByPrefixAsync(prefix);
            return Ok($"All cache with prefix '{prefix}' removed successfully.");
        }
    }
}
