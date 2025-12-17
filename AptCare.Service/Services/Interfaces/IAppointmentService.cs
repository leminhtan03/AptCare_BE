using AptCare.Repository.Enum;
using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.AppointmentDtos;
namespace AptCare.Service.Services.Interfaces
{
    public interface IAppointmentService
    {
        Task<string> CreateAppointmentAsync(AppointmentCreateDto dto);
        Task<string> CreateAppointmentWithOldTechnicianAsync(AppointmentCreateDto dto);
        Task<string> UpdateAppointmentAsync(int id, AppointmentUpdateDto dto);
        Task<string> DeleteAppointmentAsync(int id);
        Task<AppointmentDto> GetAppointmentByIdAsync(int id);
        Task<IPaginate<AppointmentDto>> GetPaginateAppointmentAsync(PaginateDto dto, DateOnly? fromDate, DateOnly? toDate, bool? isAprroved);
        Task<IEnumerable<ResidentAppointmentScheduleDto>> GetResidentAppointmentScheduleAsync(DateOnly fromDate, DateOnly toDate);
        Task<IEnumerable<TechnicianAppointmentScheduleDto>> GetTechnicianAppointmentScheduleAsync(int? technicianId, DateOnly fromDate, DateOnly toDate);
        Task<IEnumerable<TechnicianAppointmentScheduleDto>> GetMyTechnicianAppointmentScheduleAsync(DateOnly fromDate, DateOnly toDate);
        Task<bool> CheckInAsync(int id);
        Task<bool> StartRepairAsync(int id);
        Task<bool> ToogleAppoimnentStatus(int Id, string note, AppointmentStatus appointmentStatus);
        Task<string> CompleteAppointmentAsync(int id, string note, bool hasNextAppointment, DateOnly? acceptanceTime);
    }
}
