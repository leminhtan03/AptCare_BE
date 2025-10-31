using AptCare.Repository;
using AptCare.Repository.Paginate;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.RepairReportDtos;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace AptCare.Service.Services.Implements
{
    public class RepairReportService : BaseService<RepairReportService>, IRepairReportService
    {
        public RepairReportService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<RepairReportService> logger, IMapper mapper) : base(unitOfWork, logger, mapper)
        {
        }

        public Task<RepairReportDto> GenerateRepairReportAsync(CreateRepairReportDto dto)
        {
            throw new NotImplementedException();
        }

        public Task<RepairReportDto> GetRepairReportByIdAsync(int repairReportId)
        {
            throw new NotImplementedException();
        }

        public Task<IPaginate<RepairReportBasicDto>> GetRepairReportsByApartmentIdAsync(int apartmentId)
        {
            throw new NotImplementedException();
        }
    }
}
