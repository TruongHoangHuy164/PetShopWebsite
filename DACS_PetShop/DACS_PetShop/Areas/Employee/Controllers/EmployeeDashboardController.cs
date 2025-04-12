using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DACS_PetShop.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = "Employee")]
    public class EmployeeDashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
