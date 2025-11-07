using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.BuildingDtos;
using AptCare.Service.Dtos.UserDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Interfaces
{
    public interface IFloorService
    {
        Task<string> CreateFloorAsync(FloorCreateDto dto);
        Task<string> UpdateFloorAsync(int id, FloorUpdateDto dto);
        Task<string> DeleteFloorAsync(int id);
        Task<FloorDto> GetFloorByIdAsync(int id);
        Task<IEnumerable<FloorBasicDto>> GetFloorsAsync();
        Task<IPaginate<GetAllFloorsDto>> GetPaginateFloorAsync(PaginateDto dto);
    }
}
