using Microsoft.AspNetCore.Mvc;

namespace TravelPlanningSystem.Controllers;

public class BookingController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}