using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.MaintenanceTaskDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Interfaces
{
    public interface IMaintenanceTaskService
    {
        Task<string> CreateMaintenanceTaskAsync(MaintenanceTaskCreateDto dto);
        Task<string> UpdateMaintenanceTaskAsync(int id, MaintenanceTaskUpdateDto dto);
        Task<string> DeleteMaintenanceTaskAsync(int id);
        Task<string> ActivateMaintenanceTaskAsync(int id);
        Task<string> DeactivateMaintenanceTaskAsync(int id);
        Task<MaintenanceTaskDto> GetMaintenanceTaskByIdAsync(int id);
        Task<IEnumerable<MaintenanceTaskBasicDto>> GetMaintenanceTasksByTypeAsync(int commonAreaObjectTypeId);
    }
}
