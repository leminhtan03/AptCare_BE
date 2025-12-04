using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.CommonAreaObjectDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Interfaces
{
    public interface ICommonAreaObjectService
    {
        Task<string> CreateCommonAreaObjectAsync(CommonAreaObjectCreateDto dto);
        Task<string> UpdateCommonAreaObjectAsync(int id, CommonAreaObjectUpdateDto dto);
        Task<string> DeleteCommonAreaObjectAsync(int id);
        Task<string> ActivateCommonAreaObjectAsync(int id);
        Task<string> DeactivateCommonAreaObjectAsync(int id);
        Task<CommonAreaObjectDto> GetCommonAreaObjectByIdAsync(int id);
        Task<IPaginate<CommonAreaObjectDto>> GetPaginateCommonAreaObjectAsync(PaginateDto dto, int? commonAreaId);
        Task<IEnumerable<CommonAreaObjectBasicDto>> GetCommonAreaObjectsByCommonAreaAsync(int commonAreaId); 
        Task<IEnumerable<CommonAreaObjectBasicDto>> GetCommonAreaObjectsByTypeAsync(int typeId);
    }
}