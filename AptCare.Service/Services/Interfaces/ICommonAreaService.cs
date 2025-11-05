using AptCare.Repository.Paginate;
using AptCare.Service.Dtos.BuildingDtos;
using AptCare.Service.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Interfaces
{
    public interface ICommonAreaService
    {
        Task<string> CreateCommonAreaAsync(CommonAreaCreateDto dto);
        Task<string> UpdateCommonAreaAsync(int id, CommonAreaUpdateDto dto);
        Task<string> DeleteCommonAreaAsync(int id);
        Task<CommonAreaDto> GetCommonAreaByIdAsync(int id);
        Task<IEnumerable<CommonAreaDto>> GetCommonAreasAsync();
        Task<IPaginate<CommonAreaDto>> GetPaginateCommonAreaAsync(PaginateDto dto);
    }
}
