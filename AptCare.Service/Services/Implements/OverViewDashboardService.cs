using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AptCare.Service.Services.Implements
{
    public class OverViewDashboardService : BaseService<OverViewDashboardService>, IOverViewDashboardService
    {
        public OverViewDashboardService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<OverViewDashboardService> logger, IMapper mapper) : base(unitOfWork, logger, mapper)
        {
        }
    }
}
