using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.RepairRequestDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Interfaces
{
    public interface IRepairRequestService
    {
        Task<string> CreateNormalRepairRequestAsync(RepairRequestNormalCreateDto dto);
        Task<string> CreateEmergencyRepairRequestAsync(RepairRequestEmergencyCreateDto dto);
        Task<IPaginate<RepairRequestDto>> GetPaginateRepairRequestAsync(PaginateDto dto, bool? isEmergency, int? apartmentId, int? issueId, int? maintenanceRequestId);
    }
}
