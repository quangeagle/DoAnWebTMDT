using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


public class AdminController : Controller
{
    public IActionResult Dashboard()
    {
        return View();
    }
}

