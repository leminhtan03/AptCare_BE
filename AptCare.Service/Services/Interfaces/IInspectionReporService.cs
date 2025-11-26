

using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.InspectionReporDtos;

namespace AptCare.Service.Services.Interfaces
{
    public interface IInspectionReporService
    {
        Task<InspectionReportDto> CreateInspectionReportAsync(CreateInspectionReporDto dto);
        Task<IPaginate<InspectionReportDetailDto>> GetPaginateInspectionReportsAsync(InspectionReportFilterDto filterDto);
        Task<InspectionReportDetailDto> GetInspectionReportByIdAsync(int id);
        Task<ICollection<InspectionReportDto>> GetInspectionReportByAppointmentIdAsync(int id);
        Task<string> UpdateInspectionReportAsync(int id, UpdateInspectionReporDto dto);
    }
}
