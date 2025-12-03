using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AptCare.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BudgetController : ControllerBase
    {
        private readonly IBudgetService _budgetService;

        public BudgetController(IBudgetService budgetService)
        {
            _budgetService = budgetService;
        }

        /// <summary>
        /// Lấy thông tin ngân sách hiện tại.
        /// </summary>
        /// <returns>BudgetDto</returns>
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetBudget()
        {
            var budget = await _budgetService.GetBudgetAsync();
            if (budget == null)
                return NotFound("Không tìm thấy ngân sách.");
            return Ok(budget);
        }
    }
}