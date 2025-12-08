using AptCare.Repository;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace AptCare.Service.Services.Implements
{
    internal class OverViewDashboardService : BaseService<OverViewDashboardService>, IOverViewDashboardService
    {
        public OverViewDashboardService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<OverViewDashboardService> logger, IMapper mapper) : base(unitOfWork, logger, mapper)
        {
        }
    }
}
