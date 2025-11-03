using AptCare.Repository.Paginate;
using AptCare.Service.Dtos.RepairReportDtos;

namespace AptCare.Service.Services.Interfaces
{
    public interface IRepairReportService
    {
        Task<RepairReportDto> CreateRepairReportAsync(CreateRepairReportDto dto);
        Task<RepairReportDto> GetRepairReportByIdAsync(int id);
        Task<RepairReportDto> GetRepairReportByAppointmentIdAsync(int appointmentId);
        Task<IPaginate<RepairReportBasicDto>> GetPaginateRepairReportsAsync(RepairReportFilterDto filterDto);
        Task<string> UpdateRepairReportAsync(int id, UpdateRepairReportDto dto);
    }
}
