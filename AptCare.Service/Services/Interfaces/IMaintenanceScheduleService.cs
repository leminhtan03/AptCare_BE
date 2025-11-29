using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.MaintenanceScheduleDtos;

namespace AptCare.Service.Services.Interfaces
{
    public interface IMaintenanceScheduleService
    {
        Task<string> CreateMaintenanceScheduleAsync(MaintenanceScheduleCreateDto dto);
        Task<string> UpdateMaintenanceScheduleAsync(int id, MaintenanceScheduleUpdateDto dto);
        Task<string> DeleteMaintenanceScheduleAsync(int id);
        Task<string> ActivateMaintenanceScheduleAsync(int id);
        Task<string> InactiveMaintenanceScheduleAsync(int id);
        Task<MaintenanceScheduleDto> GetMaintenanceScheduleByIdAsync(int id);
        Task<IPaginate<MaintenanceScheduleDto>> GetPaginateMaintenanceScheduleAsync(PaginateDto dto, int? commonAreaObjectId);
        Task<MaintenanceScheduleDto?> GetByCommonAreaObjectIdAsync(int commonAreaObjectId);
        Task<IEnumerable<MaintenanceTrackingHistoryDto>> GetTrackingHistoryAsync(int maintenanceScheduleId);
    }
}
