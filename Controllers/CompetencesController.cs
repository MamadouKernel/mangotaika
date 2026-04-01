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
        ViewBag.Scouts = await db.Scouts.Where(s => s.IsActive).OrderBy(s => s.Nom).ThenBy(s => s.Prenom).ToListAsync();
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
            ModelState.AddModelError(nameof(model.Nom), "Le nom de la competence est requis.");
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
        TempData["Success"] = "Competence mise a jour.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, Authorize(Roles = "Administrateur,Gestionnaire"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Competence model)
    {
        model.Id = Guid.NewGuid();
        db.Competences.Add(model);
        await db.SaveChangesAsync();
        TempData["Success"] = "Competence ajoutee.";
        return RedirectToAction(nameof(Index), new { scoutId = model.ScoutId });
    }

    [HttpPost, Authorize(Roles = "Administrateur,Gestionnaire"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var c = await db.Competences.FindAsync(id);
        if (c is not null)
        {
            db.Competences.Remove(c);
            await db.SaveChangesAsync();
        }
        TempData["Success"] = "Competence supprimee.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    public async Task<IActionResult> Progression(Guid id)
    {
        var scout = await db.Scouts
            .Include(s => s.Groupe)
            .Include(s => s.Branche)
            .Include(s => s.Competences)
            .Include(s => s.HistoriqueFonctions).ThenInclude(h => h.Groupe)
            .Include(s => s.SuivisAcademiques)
            .Include(s => s.EtapesParcours)
            .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);
        if (scout is null) return NotFound();

        var etapes = scout.EtapesParcours
            .OrderBy(e => e.OrdreAffichage)
            .ThenBy(e => e.DatePrevisionnelle ?? DateTime.MaxValue)
            .ThenBy(e => e.NomEtape)
            .ToList();

        ViewBag.EtapesValidees = etapes.Where(e => e.DateValidation.HasValue).ToList();
        ViewBag.EtapesRestantes = etapes.Where(e => !e.DateValidation.HasValue).ToList();
        ViewBag.ProchaineEtape = etapes
            .Where(e => !e.DateValidation.HasValue && e.DatePrevisionnelle.HasValue)
            .OrderBy(e => e.DatePrevisionnelle)
            .FirstOrDefault();

        ViewBag.ActivitesParticipees = await db.ParticipantsActivite
            .Include(p => p.Activite).ThenInclude(a => a.Groupe)
            .Where(p => p.ScoutId == id)
            .OrderByDescending(p => p.Activite.DateDebut)
            .ToListAsync();

        return View(scout);
    }

    [HttpPost, Authorize(Roles = "Administrateur,Gestionnaire"), ValidateAntiForgeryToken]
    public async Task<IActionResult> AjouterEtapeParcours(EtapeParcoursScout model)
    {
        var scout = await db.Scouts.Include(s => s.EtapesParcours).FirstOrDefaultAsync(s => s.Id == model.ScoutId && s.IsActive);
        if (scout is null)
        {
            return NotFound();
        }

        var nomEtape = NormalizeStageName(model.NomEtape);
        if (string.IsNullOrWhiteSpace(nomEtape))
        {
            TempData["Error"] = "Le nom de l'etape du parcours est requis.";
            return RedirectToAction(nameof(Progression), new { id = model.ScoutId });
        }

        if (scout.EtapesParcours.Any(e => DatabaseText.NormalizeSearchKey(e.NomEtape) == DatabaseText.NormalizeSearchKey(nomEtape)))
        {
            TempData["Error"] = "Cette etape du parcours existe deja pour ce scout.";
            return RedirectToAction(nameof(Progression), new { id = model.ScoutId });
        }

        var prochainePosition = scout.EtapesParcours.Any() ? scout.EtapesParcours.Max(e => e.OrdreAffichage) + 1 : 1;
        var etape = new EtapeParcoursScout
        {
            Id = Guid.NewGuid(),
            ScoutId = model.ScoutId,
            NomEtape = nomEtape,
            CodeEtape = string.IsNullOrWhiteSpace(model.CodeEtape) ? null : model.CodeEtape.Trim(),
            OrdreAffichage = model.OrdreAffichage > 0 ? model.OrdreAffichage : prochainePosition,
            DateValidation = NormalizeDate(model.DateValidation),
            DatePrevisionnelle = NormalizeDate(model.DatePrevisionnelle),
            Observations = NormalizeNotes(model.Observations),
            EstObligatoire = model.EstObligatoire
        };

        db.EtapesParcoursScouts.Add(etape);
        await db.SaveChangesAsync();
        TempData["Success"] = "Etape du parcours ajoutee.";
        return RedirectToAction(nameof(Progression), new { id = model.ScoutId });
    }

    [HttpPost, Authorize(Roles = "Administrateur,Gestionnaire"), ValidateAntiForgeryToken]
    public async Task<IActionResult> MettreAJourEtapeParcours(Guid id, DateTime? dateValidation, DateTime? datePrevisionnelle, string? observations, int ordreAffichage)
    {
        var etape = await db.EtapesParcoursScouts.FirstOrDefaultAsync(e => e.Id == id);
        if (etape is null)
        {
            return NotFound();
        }

        etape.DateValidation = NormalizeDate(dateValidation);
        etape.DatePrevisionnelle = NormalizeDate(datePrevisionnelle);
        etape.Observations = NormalizeNotes(observations);
        etape.OrdreAffichage = ordreAffichage <= 0 ? etape.OrdreAffichage : ordreAffichage;
        await db.SaveChangesAsync();
        TempData["Success"] = "Etape du parcours mise a jour.";
        return RedirectToAction(nameof(Progression), new { id = etape.ScoutId });
    }

    [HttpPost, Authorize(Roles = "Administrateur,Gestionnaire"), ValidateAntiForgeryToken]
    public async Task<IActionResult> SupprimerEtapeParcours(Guid id)
    {
        var etape = await db.EtapesParcoursScouts.FirstOrDefaultAsync(e => e.Id == id);
        if (etape is null)
        {
            return NotFound();
        }

        var scoutId = etape.ScoutId;
        db.EtapesParcoursScouts.Remove(etape);
        await db.SaveChangesAsync();
        TempData["Success"] = "Etape du parcours supprimee.";
        return RedirectToAction(nameof(Progression), new { id = scoutId });
    }

    [HttpPost, Authorize(Roles = "Administrateur,Gestionnaire"), ValidateAntiForgeryToken]
    public async Task<IActionResult> AjouterSuiviAcademique(SuiviAcademique model)
    {
        model.Id = Guid.NewGuid();
        model.DateSaisie = DateTime.UtcNow;
        db.SuivisAcademiques.Add(model);
        await db.SaveChangesAsync();
        TempData["Success"] = "Suivi academique ajoute.";
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
            TempData["Success"] = "Suivi academique supprime.";
            return RedirectToAction(nameof(Progression), new { id = scoutId });
        }
        return RedirectToAction(nameof(Index));
    }

    private static string NormalizeStageName(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string? NormalizeNotes(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static DateTime? NormalizeDate(DateTime? value)
    {
        return value.HasValue ? DateTime.SpecifyKind(value.Value.Date, DateTimeKind.Utc) : null;
    }
}
