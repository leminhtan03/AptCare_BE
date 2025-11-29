using AptCare.Repository.Enum;
using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.RepairRequestDtos;

namespace AptCare.Service.Services.Interfaces
{
    public interface IRepairRequestService
    {
        Task<string> CreateNormalRepairRequestAsync(RepairRequestNormalCreateDto dto);
        Task<string> CreateEmergencyRepairRequestAsync(RepairRequestEmergencyCreateDto dto);
        Task<IPaginate<RepairRequestDto>> GetPaginateRepairRequestAsync(PaginateDto dto, bool? isEmergency, int? apartmentId, int? issueId, bool? isMaintain);
        Task<RepairRequestDetailDto> GetRepairRequestByIdAsync(int id);
        Task<bool> ToggleRepairRequestStatusAsync(ToggleRRStatus dto);
        Task<string> ApprovalRepairRequestAsync(ToggleRRStatus dto);
        Task CheckAcceptanceTimeAsync(DateTime dateTime);
        Task CheckMaintenanceScheduleAsync(DateTime now);
        Task<IEnumerable<RepairRequestDto>> GetRepairRequestsByMaintenanceScheduleIdAsync(int maintenanceScheduleId);
    }
}
