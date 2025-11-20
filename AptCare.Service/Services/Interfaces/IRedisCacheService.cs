using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Interfaces
{
    public interface IRedisCacheService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan expiration);
        Task RemoveAsync(string key);
        Task<IEnumerable<string>> GetKeysAsync(string pattern); // Add this method
        Task RemoveByPrefixAsync(string prefix);
        Task ClearAllCacheAsync();
    }
}
