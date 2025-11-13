using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.AccessoryDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Interfaces
{
    public interface IAccessoryService
    {
        Task<string> CreateAccessoryAsync(AccessoryCreateDto dto);
        Task<string> UpdateAccessoryAsync(int id, AccessoryUpdateDto dto);
        Task<string> DeleteAccessoryAsync(int id);
        Task<AccessoryDto> GetAccessoryByIdAsync(int id);
        Task<IPaginate<AccessoryDto>> GetPaginateAccessoryAsync(PaginateDto dto);
        Task<IEnumerable<AccessoryDto>> GetAccessoriesAsync();
    }
}
