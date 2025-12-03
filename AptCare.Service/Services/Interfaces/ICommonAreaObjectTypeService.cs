using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.CommonAreaObjectTypeDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Interfaces
{
    public interface ICommonAreaObjectTypeService
    {
        Task<string> CreateCommonAreaObjectTypeAsync(CommonAreaObjectTypeCreateDto dto);
        Task<string> UpdateCommonAreaObjectTypeAsync(int id, CommonAreaObjectTypeUpdateDto dto);
        Task<string> DeleteCommonAreaObjectTypeAsync(int id);
        Task<string> ActivateCommonAreaObjectTypeAsync(int id);
        Task<string> DeactivateCommonAreaObjectTypeAsync(int id);
        Task<CommonAreaObjectTypeDto> GetCommonAreaObjectTypeByIdAsync(int id);
        Task<IPaginate<CommonAreaObjectTypeDto>> GetPaginateCommonAreaObjectTypeAsync(PaginateDto dto);
        Task<IEnumerable<CommonAreaObjectTypeDto>> GetCommonAreaObjectTypesAsync();
    }
}
