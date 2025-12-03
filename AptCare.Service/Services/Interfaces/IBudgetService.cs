using System.Threading.Tasks;
using AptCare.Service.Dtos.BudgetDtos;

namespace AptCare.Service.Services.Interfaces
{
    public interface IBudgetService
    {
        Task<BudgetDto> GetBudgetAsync();
    }
}