using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.AppointmentDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Interfaces
{
    public interface IAppointmentService
    {
        Task<string> CreateAppointmentAsync(AppointmentCreateDto dto);
        Task<string> UpdateAppointmentAsync(int id, AppointmentUpdateDto dto);
        Task<string> DeleteAppointmentAsync(int id);
        Task<AppointmentDto> GetAppointmentByIdAsync(int id);
        Task<IPaginate<AppointmentDto>> GetPaginateAppointmentAsync(PaginateDto dto, DateOnly? fromDate, DateOnly? toDate);
        Task<IEnumerable<ResidentAppointmentScheduleDto>> GetResidentAppointmentScheduleAsync(DateOnly fromDate, DateOnly toDate);
        Task<IEnumerable<TechnicianAppointmentScheduleDto>> GetTechnicianAppointmentScheduleAsync(int? technicianId, DateOnly fromDate, DateOnly toDate);
        Task<IEnumerable<TechnicianAppointmentScheduleDto>> GetMyTechnicianAppointmentScheduleAsync(DateOnly fromDate, DateOnly toDate);
        Task CheckInAsync(int id);
        Task StartRepairAsync(int id);
    }
}
