using Microsoft.AspNetCore.Mvc;

namespace DoAnWebTMDT.Controllers
{
    public class UserController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("CustomerIndex", "Products");
        }

    }
}
