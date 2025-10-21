using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    public class SlotController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
