using AptCare.Repository.Enum;
using AptCare.Service.Dtos.WorkSlotDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Interfaces
{
    public interface IWorkSlotService
    {
        Task<string> CreateWorkSlotsFromDateToDateAsync(WorkSlotCreateFromDateToDateDto dto);
        Task<string> CreateWorkSlotsDateSlotAsync(WorkSlotCreateDateSlotDto dto);
        Task<string> UpdateWorkSlotAsync(int id, WorkSlotUpdateDto dto);
        Task<string> DeleteWorkSlotAsync(int id);
        Task<IEnumerable<WorkSlotDto>> GetTechnicianScheduleAsync(int? technicianId, DateOnly fromDate, DateOnly toDate, WorkSlotStatus? status);
        Task<IEnumerable<WorkSlotDto>> GetMyScheduleAsync(DateOnly fromDate, DateOnly toDate, WorkSlotStatus? status);
        Task<string> CheckInAsync(DateOnly date, int slotId);
        Task<string> CheckOutAsync(DateOnly date, int slotId);
    }
}
