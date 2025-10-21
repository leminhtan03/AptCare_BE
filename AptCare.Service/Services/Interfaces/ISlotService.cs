using AptCare.Repository.Paginate;
using AptCare.Service.Dtos.BuildingDtos;
using AptCare.Service.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AptCare.Service.Dtos.SlotDtos;

namespace AptCare.Service.Services.Interfaces
{
    public interface ISlotService
    {
        Task<string> CreateSlotAsync(SlotCreateDto dto);
        Task<string> UpdateSlotAsync(int id, SlotUpdateDto dto);
        Task<string> DeleteSlotAsync(int id);
        Task<SlotDto> GetSlotByIdAsync(int id);
        Task<IEnumerable<SlotDto>> GetSlotsAsync();
        Task<IPaginate<SlotDto>> GetPaginateSlotAsync(PaginateDto dto);
    }
}
