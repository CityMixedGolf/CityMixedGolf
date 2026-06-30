using Microsoft.AspNetCore.Mvc;

namespace CityMixedGolf.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Home", new { area = "Public" });
    }

    public IActionResult Error()
    {
        return View();
    }
}