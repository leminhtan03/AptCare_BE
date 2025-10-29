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
    public interface IApartmentService
    {
        Task<string> CreateApartmentAsync(ApartmentCreateDto dto);
        Task<string> UpdateApartmentAsync(int id, ApartmentUpdateDto dto);
        Task<string> DeleteApartmentAsync(int id);
        Task<ApartmentDto> GetApartmentByIdAsync(int id);
        Task<IPaginate<ApartmentDto>> GetPaginateApartmentAsync(PaginateDto dto, int? floorId);
        Task<IEnumerable<ApartmentDto>> GetApartmentsByFloorAsync(int floorId);
        Task<ApartmentDto> UpadteUserDataForAptAsync(int AptId, UpdateApartmentWithResidentDataDto dto);
    }
}
