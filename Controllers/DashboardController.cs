using MangoTaika.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MangoTaika.Controllers;

[Authorize]
public class DashboardController(IDashboardService dashboardService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var data = await dashboardService.GetDashboardAsync(User);
        return View(data);
    }
}
