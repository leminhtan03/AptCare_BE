using AptCare.Repository;
using AptCare.Service.Dtos.BudgetDtos;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Logging;
using AptCare.Repository.UnitOfWork;
using AptCare.Repository.Entities;

namespace AptCare.Service.Services.Implements
{
    public class BudgetService : BaseService<BudgetService>, IBudgetService
    {
        public BudgetService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<BudgetService> logger, IMapper mapper) : base(unitOfWork, logger, mapper)
        {
        }

        public async Task<BudgetDto> GetBudgetAsync()
        {
            var budget = await _unitOfWork.GetRepository<Budget>().SingleOrDefaultAsync();
            if (budget == null)
            {
                _logger.LogWarning("No budget found in the system.");
                return null;
            }
            return _mapper.Map<BudgetDto>(budget);
        }
    }
}