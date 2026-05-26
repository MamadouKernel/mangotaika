using MangoTaika.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MangoTaika.Controllers;

[Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
public class RolesController : Controller
{
    public IActionResult Index()
    {
        return View(RoleNames.Definitions
            .OrderBy(r => r.Hierarchy)
            .ThenBy(r => r.Label)
            .ToList());
    }
}
