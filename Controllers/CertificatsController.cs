using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

[Authorize]
public class CertificatsController(
    AppDbContext db,
    UserManager<ApplicationUser> userManager) : Controller
{
    [Authorize(Roles = "Scout")]
    public async Task<IActionResult> MesCertificats()
    {
        var scout = await GetCurrentScoutAsync();
        if (scout is null)
            return RedirectToAction("Index", "Dashboard");

        var certificats = await db.CertificationsFormation
            .AsNoTracking()
            .Where(c => c.ScoutId == scout.Id)
            .Include(c => c.Formation)
            .OrderByDescending(c => c.DateEmission)
            .Select(c => new CertificationFormationDto
            {
                Id = c.Id,
                Type = c.Type,
                Code = c.Code,
                DateEmission = c.DateEmission,
                ScoreFinal = c.ScoreFinal,
                Mention = c.Mention,
                FormationId = c.FormationId,
                FormationTitre = c.Formation.Titre,
                NomScout = scout.Prenom + " " + scout.Nom
            })
            .ToListAsync();

        return View(certificats);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var certificat = await GetAccessibleCertificateAsync(id);
        if (certificat is null)
            return Forbid();

        return View(certificat);
    }

    public async Task<IActionResult> Imprimer(Guid id)
    {
        var certificat = await GetAccessibleCertificateAsync(id);
        if (certificat is null)
            return Forbid();

        return View("Print", certificat);
    }

    private async Task<CertificationFormationDto?> GetAccessibleCertificateAsync(Guid id)
    {
        var currentScout = await GetCurrentScoutAsync();
        var canReviewAll = User.IsInRole("Administrateur") || User.IsInRole("Gestionnaire") || User.IsInRole("Superviseur") || User.IsInRole("Consultant");

        var certificat = await db.CertificationsFormation
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Include(c => c.Formation)
            .Include(c => c.Scout)
            .Select(c => new CertificationFormationDto
            {
                Id = c.Id,
                Type = c.Type,
                Code = c.Code,
                DateEmission = c.DateEmission,
                ScoreFinal = c.ScoreFinal,
                Mention = c.Mention,
                FormationId = c.FormationId,
                FormationTitre = c.Formation.Titre,
                NomScout = c.Scout.Prenom + " " + c.Scout.Nom
            })
            .FirstOrDefaultAsync();

        if (certificat is null)
            return null;

        if (canReviewAll)
            return certificat;

        if (currentScout is null)
            return null;

        var ownsCertificate = await db.CertificationsFormation.AnyAsync(c => c.Id == id && c.ScoutId == currentScout.Id);
        return ownsCertificate ? certificat : null;
    }

    private async Task<Scout?> GetCurrentScoutAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return null;

        return await db.Scouts.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == user.Id && s.IsActive);
    }
}
