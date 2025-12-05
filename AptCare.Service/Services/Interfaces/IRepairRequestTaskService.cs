using AptCare.Service.Dtos.InspectionReporDtos;
using AptCare.Service.Dtos.RepairRequestTaskDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Interfaces
{
    public interface IRepairRequestTaskService
    {
        Task<string> UpdateRepairRequestTaskStatusAsync(int id, RepairRequestTaskStatusUpdateDto dto);
        Task<RepairRequestTaskDto> GetRepairRequestTaskByIdAsync(int id);
        Task<IEnumerable<RepairRequestTaskDto>> GetRepairRequestTasksByRepairRequestIdAsync(int repairRequestId);
        Task<string> UpdateRepairRequestTasksStatusAsync(int repairRequestId, List<RequestTaskStatusUpdateDto> updatedTasks);
    }
}
