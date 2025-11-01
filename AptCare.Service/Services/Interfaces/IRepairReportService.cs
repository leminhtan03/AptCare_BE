using AptCare.Repository.Paginate;
using AptCare.Service.Dtos.RepairReportDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Interfaces
{
    public interface IRepairReportService
    {
        public Task<RepairReportDto> GenerateRepairReportAsync(CreateRepairReportDto dto);
        public Task<RepairReportDto> GetRepairReportByIdAsync(int repairReportId);
        public Task<IPaginate<RepairReportBasicDto>> GetRepairReportsByApartmentIdAsync(int apartmentId);
    }
}
