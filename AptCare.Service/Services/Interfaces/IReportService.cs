using AptCare.Repository.Paginate;
using AptCare.Service.Dtos.ReportDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Interfaces
{
    public interface IReportService
    {
        Task<ReportDto> CreateReportAsync(ReportCreateDto dto);
        Task<string> UpdateReportAsync(int id, ReportUpdateDto dto);
        Task<string> DeleteReportAsync(int id);
        Task<ReportDto> GetReportByIdAsync(int id);
        Task<IPaginate<ReportDto>> GetPaginateReportsAsync(ReportFilterDto filterDto);
        Task<IEnumerable<ReportBasicDto>> GetReportsByCommonAreaObjectAsync(int commonAreaObjectId);
        Task<IPaginate<ReportDto>> GetMyReportsAsync(ReportFilterDto filterDto);
        Task<string> ActivateReportAsync(int id);
        Task<string> DeactivateReportAsync(int id);
    }
}
