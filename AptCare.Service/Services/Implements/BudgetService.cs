using AptCare.Repository;
using AptCare.Service.Dtos.BudgetDtos;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace AptCare.Service.Services.Implements
{
    public class BudgetService : IBudgetService
    {
        private readonly AptCareSystemDBContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<BudgetService> _logger;

        public BudgetService(
            AptCareSystemDBContext context,
            IMapper mapper,
            ILogger<BudgetService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<BudgetDto> GetBudgetAsync()
        {
            var budget = await _context.Budgets.AsNoTracking().FirstOrDefaultAsync();
            if (budget == null)
            {
                _logger.LogWarning("No budget found in the system.");
                return null;
            }
            return _mapper.Map<BudgetDto>(budget);
        }
    }
}