using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
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

        var referentielEtapes = scout.BrancheId.HasValue
            ? await db.ModelesEtapesParcours
                .Where(m => m.IsActive && m.BrancheId == scout.BrancheId.Value)
                .OrderBy(m => m.OrdreAffichage)
                .ThenBy(m => m.NomEtape)
                .ToListAsync()
            : [];

        var etapes = BuildProgressionEtapes(scout, referentielEtapes);
        var etapesValidees = etapes.Where(e => e.DateValidation.HasValue).ToList();
        var etapesRestantes = etapes.Where(e => !e.DateValidation.HasValue).ToList();
        var prochaineEtape = etapesRestantes
            .Where(e => e.DatePrevisionnelle.HasValue)
            .OrderBy(e => e.DatePrevisionnelle)
            .ThenBy(e => e.OrdreAffichage)
            .ThenBy(e => e.NomEtape)
            .FirstOrDefault()
            ?? etapesRestantes
                .OrderBy(e => e.OrdreAffichage)
                .ThenBy(e => e.NomEtape)
                .FirstOrDefault();

        var activitesParticipees = await db.ParticipantsActivite
            .Include(p => p.Activite).ThenInclude(a => a.Groupe)
            .Where(p => p.ScoutId == id)
            .OrderByDescending(p => p.Activite.DateDebut)
            .ToListAsync();

        var prochainePosition = etapes.Any()
            ? etapes.Max(e => e.OrdreAffichage) + 1
            : (referentielEtapes.Any() ? referentielEtapes.Max(e => e.OrdreAffichage) + 1 : 1);

        var model = new ScoutProgressionViewModel
        {
            Scout = scout,
            Etapes = etapes,
            EtapesValidees = etapesValidees,
            EtapesRestantes = etapesRestantes,
            ProchaineEtape = prochaineEtape,
            ActivitesParticipees = activitesParticipees,
            ReferentielEtapes = referentielEtapes,
            ProchainePosition = prochainePosition
        };

        return View(model);
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
        var codeEtape = NormalizeStageCode(model.CodeEtape);
        if (string.IsNullOrWhiteSpace(nomEtape))
        {
            TempData["Error"] = "Le nom de l'etape du parcours est requis.";
            return RedirectToAction(nameof(Progression), new { id = model.ScoutId });
        }

        if (scout.EtapesParcours.Any(e => AreSameStage(e.CodeEtape, e.NomEtape, codeEtape, nomEtape)))
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
            CodeEtape = codeEtape,
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
    public async Task<IActionResult> EnregistrerEtapeParcours(EnregistrerEtapeParcoursDto model)
    {
        var scout = await db.Scouts
            .Include(s => s.EtapesParcours)
            .FirstOrDefaultAsync(s => s.Id == model.ScoutId && s.IsActive);
        if (scout is null)
        {
            return NotFound();
        }

        var etapeExistante = model.EtapeParcoursId.HasValue
            ? scout.EtapesParcours.FirstOrDefault(e => e.Id == model.EtapeParcoursId.Value)
            : null;

        ModeleEtapeParcours? modeleEtape = null;
        if (etapeExistante is null && model.ModeleEtapeParcoursId.HasValue)
        {
            modeleEtape = await db.ModelesEtapesParcours
                .FirstOrDefaultAsync(m => m.Id == model.ModeleEtapeParcoursId.Value && m.IsActive);
            if (modeleEtape is null)
            {
                TempData["Error"] = "Le referentiel de l'etape est introuvable.";
                return RedirectToAction(nameof(Progression), new { id = model.ScoutId });
            }

            if (modeleEtape.BrancheId.HasValue && scout.BrancheId != modeleEtape.BrancheId)
            {
                TempData["Error"] = "Cette etape officielle ne correspond pas a la branche du scout.";
                return RedirectToAction(nameof(Progression), new { id = model.ScoutId });
            }
        }

        var nomEtape = NormalizeStageName(model.NomEtape);
        if (string.IsNullOrWhiteSpace(nomEtape))
        {
            nomEtape = modeleEtape?.NomEtape ?? string.Empty;
        }

        var codeEtape = NormalizeStageCode(model.CodeEtape);
        if (string.IsNullOrWhiteSpace(codeEtape))
        {
            codeEtape = NormalizeStageCode(modeleEtape?.CodeEtape);
        }

        if (string.IsNullOrWhiteSpace(nomEtape))
        {
            TempData["Error"] = "Le nom de l'etape du parcours est requis.";
            return RedirectToAction(nameof(Progression), new { id = model.ScoutId });
        }

        etapeExistante ??= scout.EtapesParcours.FirstOrDefault(e => AreSameStage(e.CodeEtape, e.NomEtape, codeEtape, nomEtape));

        var doublon = scout.EtapesParcours.FirstOrDefault(e =>
            (etapeExistante is null || e.Id != etapeExistante.Id)
            && AreSameStage(e.CodeEtape, e.NomEtape, codeEtape, nomEtape));
        if (doublon is not null)
        {
            TempData["Error"] = "Cette etape du parcours existe deja pour ce scout.";
            return RedirectToAction(nameof(Progression), new { id = model.ScoutId });
        }

        var isCreation = etapeExistante is null;
        if (isCreation)
        {
            etapeExistante = new EtapeParcoursScout
            {
                Id = Guid.NewGuid(),
                ScoutId = model.ScoutId,
                NomEtape = nomEtape,
                CodeEtape = codeEtape,
                OrdreAffichage = model.OrdreAffichage > 0
                    ? model.OrdreAffichage
                    : (modeleEtape?.OrdreAffichage > 0 ? modeleEtape.OrdreAffichage : scout.EtapesParcours.DefaultIfEmpty().Max(e => e?.OrdreAffichage ?? 0) + 1),
                EstObligatoire = modeleEtape?.EstObligatoire ?? model.EstObligatoire,
                DateValidation = NormalizeDate(model.DateValidation),
                DatePrevisionnelle = NormalizeDate(model.DatePrevisionnelle),
                Observations = NormalizeNotes(model.Observations)
            };
            db.EtapesParcoursScouts.Add(etapeExistante);
        }
        else
        {
            etapeExistante!.NomEtape = nomEtape;
            etapeExistante.CodeEtape = codeEtape;
            etapeExistante.OrdreAffichage = model.OrdreAffichage > 0 ? model.OrdreAffichage : etapeExistante.OrdreAffichage;
            etapeExistante.EstObligatoire = modeleEtape?.EstObligatoire ?? model.EstObligatoire;
            etapeExistante.DateValidation = NormalizeDate(model.DateValidation);
            etapeExistante.DatePrevisionnelle = NormalizeDate(model.DatePrevisionnelle);
            etapeExistante.Observations = NormalizeNotes(model.Observations);
        }

        await db.SaveChangesAsync();
        TempData["Success"] = isCreation
            ? "Etape du parcours enregistree depuis le referentiel."
            : "Etape du parcours mise a jour.";
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
    public async Task<IActionResult> AjouterModeleEtapeParcours(ModeleEtapeParcoursCreateDto model)
    {
        var scout = await db.Scouts
            .Include(s => s.Branche)
            .FirstOrDefaultAsync(s => s.Id == model.ScoutId && s.IsActive);
        if (scout is null)
        {
            return NotFound();
        }

        var brancheId = model.BrancheId ?? scout.BrancheId;
        if (!brancheId.HasValue)
        {
            TempData["Error"] = "Le scout doit etre rattache a une branche pour definir un referentiel de parcours.";
            return RedirectToAction(nameof(Progression), new { id = model.ScoutId });
        }

        var branche = await db.Branches.FirstOrDefaultAsync(b => b.Id == brancheId.Value && b.IsActive);
        if (branche is null)
        {
            TempData["Error"] = "La branche de reference est introuvable ou inactive.";
            return RedirectToAction(nameof(Progression), new { id = model.ScoutId });
        }

        var nomEtape = NormalizeStageName(model.NomEtape);
        var codeEtape = NormalizeStageCode(model.CodeEtape);
        if (string.IsNullOrWhiteSpace(nomEtape))
        {
            TempData["Error"] = "Le nom de l'etape officielle est requis.";
            return RedirectToAction(nameof(Progression), new { id = model.ScoutId });
        }

        if (await HasDuplicateModeleEtapeAsync(brancheId.Value, codeEtape, nomEtape))
        {
            TempData["Error"] = "Cette etape officielle existe deja dans le referentiel de la branche.";
            return RedirectToAction(nameof(Progression), new { id = model.ScoutId });
        }

        var dernierOrdre = await db.ModelesEtapesParcours
            .Where(m => m.IsActive && m.BrancheId == brancheId.Value)
            .OrderByDescending(m => m.OrdreAffichage)
            .Select(m => (int?)m.OrdreAffichage)
            .FirstOrDefaultAsync() ?? 0;

        var modeleEtape = new ModeleEtapeParcours
        {
            Id = Guid.NewGuid(),
            BrancheId = brancheId.Value,
            NomEtape = nomEtape,
            CodeEtape = codeEtape,
            OrdreAffichage = model.OrdreAffichage > 0 ? model.OrdreAffichage : dernierOrdre + 1,
            EstObligatoire = model.EstObligatoire,
            Description = NormalizeNotes(model.Description),
            IsActive = true
        };

        db.ModelesEtapesParcours.Add(modeleEtape);
        await db.SaveChangesAsync();
        TempData["Success"] = "Etape officielle ajoutee au referentiel de la branche.";
        return RedirectToAction(nameof(Progression), new { id = model.ScoutId });
    }

    [HttpPost, Authorize(Roles = "Administrateur,Gestionnaire"), ValidateAntiForgeryToken]
    public async Task<IActionResult> SupprimerModeleEtapeParcours(Guid id, Guid scoutId)
    {
        var modeleEtape = await db.ModelesEtapesParcours.FirstOrDefaultAsync(m => m.Id == id && m.IsActive);
        if (modeleEtape is null)
        {
            return NotFound();
        }

        modeleEtape.IsActive = false;
        await db.SaveChangesAsync();
        TempData["Success"] = "Etape officielle retiree du referentiel.";
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

    private async Task<bool> HasDuplicateModeleEtapeAsync(Guid brancheId, string? codeEtape, string nomEtape)
    {
        var modeles = await db.ModelesEtapesParcours
            .Where(m => m.IsActive && m.BrancheId == brancheId)
            .ToListAsync();

        return modeles.Any(m => AreSameStage(m.CodeEtape, m.NomEtape, codeEtape, nomEtape));
    }

    private static List<ScoutParcoursEtapeViewModel> BuildProgressionEtapes(Scout scout, IReadOnlyCollection<ModeleEtapeParcours> referentielEtapes)
    {
        var merged = new List<ScoutParcoursEtapeViewModel>();
        var usedScoutStepIds = new HashSet<Guid>();
        var scoutEtapes = scout.EtapesParcours
            .OrderBy(e => e.OrdreAffichage)
            .ThenBy(e => e.DatePrevisionnelle ?? DateTime.MaxValue)
            .ThenBy(e => e.NomEtape)
            .ToList();

        foreach (var modeleEtape in referentielEtapes.OrderBy(m => m.OrdreAffichage).ThenBy(m => m.NomEtape))
        {
            var etapeScout = scoutEtapes.FirstOrDefault(e => !usedScoutStepIds.Contains(e.Id) && AreSameStage(e.CodeEtape, e.NomEtape, modeleEtape.CodeEtape, modeleEtape.NomEtape));
            if (etapeScout is not null)
            {
                usedScoutStepIds.Add(etapeScout.Id);
                merged.Add(new ScoutParcoursEtapeViewModel
                {
                    ScoutId = scout.Id,
                    EtapeParcoursId = etapeScout.Id,
                    ModeleEtapeParcoursId = modeleEtape.Id,
                    NomEtape = etapeScout.NomEtape,
                    CodeEtape = string.IsNullOrWhiteSpace(etapeScout.CodeEtape) ? modeleEtape.CodeEtape : etapeScout.CodeEtape,
                    OrdreAffichage = etapeScout.OrdreAffichage > 0 ? etapeScout.OrdreAffichage : modeleEtape.OrdreAffichage,
                    DateValidation = etapeScout.DateValidation,
                    DatePrevisionnelle = etapeScout.DatePrevisionnelle,
                    Observations = etapeScout.Observations,
                    EstObligatoire = modeleEtape.EstObligatoire,
                    EstIssueReferentiel = true,
                    ExistePourScout = true
                });
            }
            else
            {
                merged.Add(new ScoutParcoursEtapeViewModel
                {
                    ScoutId = scout.Id,
                    ModeleEtapeParcoursId = modeleEtape.Id,
                    NomEtape = modeleEtape.NomEtape,
                    CodeEtape = modeleEtape.CodeEtape,
                    OrdreAffichage = modeleEtape.OrdreAffichage,
                    EstObligatoire = modeleEtape.EstObligatoire,
                    EstIssueReferentiel = true,
                    ExistePourScout = false
                });
            }
        }

        foreach (var etapeScout in scoutEtapes.Where(e => !usedScoutStepIds.Contains(e.Id)))
        {
            merged.Add(new ScoutParcoursEtapeViewModel
            {
                ScoutId = scout.Id,
                EtapeParcoursId = etapeScout.Id,
                NomEtape = etapeScout.NomEtape,
                CodeEtape = etapeScout.CodeEtape,
                OrdreAffichage = etapeScout.OrdreAffichage,
                DateValidation = etapeScout.DateValidation,
                DatePrevisionnelle = etapeScout.DatePrevisionnelle,
                Observations = etapeScout.Observations,
                EstObligatoire = etapeScout.EstObligatoire,
                EstIssueReferentiel = false,
                ExistePourScout = true
            });
        }

        return merged
            .OrderBy(e => e.OrdreAffichage)
            .ThenBy(e => e.DatePrevisionnelle ?? DateTime.MaxValue)
            .ThenBy(e => e.NomEtape)
            .ToList();
    }

    private static string NormalizeStageName(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string? NormalizeStageCode(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    }

    private static bool AreSameStage(string? leftCode, string leftName, string? rightCode, string rightName)
    {
        var normalizedLeftCode = NormalizeStageCode(leftCode);
        var normalizedRightCode = NormalizeStageCode(rightCode);
        if (!string.IsNullOrWhiteSpace(normalizedLeftCode) && !string.IsNullOrWhiteSpace(normalizedRightCode))
        {
            return string.Equals(normalizedLeftCode, normalizedRightCode, StringComparison.OrdinalIgnoreCase);
        }

        return DatabaseText.NormalizeSearchKey(leftName) == DatabaseText.NormalizeSearchKey(rightName);
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


