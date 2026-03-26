using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

[Authorize(Roles = "Administrateur,Gestionnaire,Superviseur,Consultant")]
public class CompetencesController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(Guid? scoutId)
    {
        var (page, ps) = ListPagination.Read(Request);
        var query = db.Competences.Include(c => c.Scout).ThenInclude(s => s.Branche).AsQueryable();
        if (scoutId.HasValue) query = query.Where(c => c.ScoutId == scoutId.Value);
        var ordered = query.OrderByDescending(c => c.DateObtention);
        var total = await ordered.CountAsync();
        var (p, pageSize, skip, totalPages) = ListPagination.Normalize(page, ps, total);
        var competences = await ordered.Skip(skip).Take(pageSize).ToListAsync();
        ViewBag.Scouts = await db.Scouts.Where(s => s.IsActive).OrderBy(s => s.Nom).ToListAsync();
        ViewBag.ScoutId = scoutId;
        ListPagination.SetViewData(ViewData, HttpContext, p, pageSize, total, totalPages);
        return View(competences);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var c = await db.Competences
            .Include(x => x.Scout).ThenInclude(s => s!.Groupe)
            .Include(x => x.Scout).ThenInclude(s => s!.Branche)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();
        return View(c);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var c = await db.Competences.Include(x => x.Scout).FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();
        ViewBag.Scouts = await db.Scouts.Where(s => s.IsActive).OrderBy(s => s.Nom).ThenBy(s => s.Prenom).ToListAsync();
        return View(c);
    }

    [HttpPost, Authorize(Roles = "Administrateur,Gestionnaire"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Competence model)
    {
        var c = await db.Competences.FindAsync(id);
        if (c is null) return NotFound();
        if (string.IsNullOrWhiteSpace(model.Nom))
        {
            ModelState.AddModelError(nameof(model.Nom), "Le nom de la compétence est requis.");
            ViewBag.Scouts = await db.Scouts.Where(s => s.IsActive).OrderBy(s => s.Nom).ThenBy(s => s.Prenom).ToListAsync();
            await db.Entry(c).Reference(x => x.Scout).LoadAsync();
            model.Id = id;
            model.Scout = c.Scout;
            return View(model);
        }
        c.Nom = model.Nom.Trim();
        c.Description = model.Description;
        c.Type = model.Type;
        c.Niveau = model.Niveau;
        c.DateObtention = model.DateObtention;
        c.ScoutId = model.ScoutId;
        await db.SaveChangesAsync();
        TempData["Success"] = "Compétence mise à jour.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, Authorize(Roles = "Administrateur,Gestionnaire"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Competence model)
    {
        model.Id = Guid.NewGuid();
        db.Competences.Add(model);
        await db.SaveChangesAsync();
        TempData["Success"] = "Compétence ajoutée.";
        return RedirectToAction(nameof(Index), new { scoutId = model.ScoutId });
    }

    [HttpPost, Authorize(Roles = "Administrateur,Gestionnaire"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var c = await db.Competences.FindAsync(id);
        if (c is not null) { db.Competences.Remove(c); await db.SaveChangesAsync(); }
        TempData["Success"] = "Compétence supprimée.";
        return RedirectToAction(nameof(Index));
    }

    // Fiche progression individuelle d'un scout
    [Authorize]
    public async Task<IActionResult> Progression(Guid id)
    {
        var scout = await db.Scouts
            .Include(s => s.Groupe).Include(s => s.Branche)
            .Include(s => s.Competences)
            .Include(s => s.HistoriqueFonctions).ThenInclude(h => h.Groupe)
            .Include(s => s.SuivisAcademiques)
            .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);
        if (scout is null) return NotFound();

        ViewBag.ActivitesParticipees = await db.ParticipantsActivite
            .Include(p => p.Activite).ThenInclude(a => a.Groupe)
            .Where(p => p.ScoutId == id)
            .OrderByDescending(p => p.Activite.DateDebut)
            .ToListAsync();

        return View(scout);
    }

    [HttpPost, Authorize(Roles = "Administrateur,Gestionnaire"), ValidateAntiForgeryToken]
    public async Task<IActionResult> AjouterSuiviAcademique(SuiviAcademique model)
    {
        model.Id = Guid.NewGuid();
        model.DateSaisie = DateTime.UtcNow;
        db.SuivisAcademiques.Add(model);
        await db.SaveChangesAsync();
        TempData["Success"] = "Suivi académique ajouté.";
        return RedirectToAction(nameof(Progression), new { id = model.ScoutId });
    }

    [HttpPost, Authorize(Roles = "Administrateur,Gestionnaire"), ValidateAntiForgeryToken]
    public async Task<IActionResult> SupprimerSuiviAcademique(Guid id)
    {
        var s = await db.SuivisAcademiques.FindAsync(id);
        if (s is not null)
        {
            var scoutId = s.ScoutId;
            db.SuivisAcademiques.Remove(s);
            await db.SaveChangesAsync();
            TempData["Success"] = "Suivi académique supprimé.";
            return RedirectToAction(nameof(Progression), new { id = scoutId });
        }
        return RedirectToAction(nameof(Index));
    }
}
