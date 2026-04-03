using ClosedXML.Excel;
using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
using MangoTaika.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;

namespace MangoTaika.Controllers;

[Authorize(Roles = "Administrateur,Gestionnaire,Superviseur,Consultant")]
public class InscriptionsAnnuellesController(AppDbContext db, UserManager<ApplicationUser> userManager, IMemoryCache memoryCache) : Controller
{
    private const string ImportReportCachePrefix = "annual-registrations-import-report:";
    private static readonly TimeSpan ImportReportLifetime = TimeSpan.FromMinutes(15);

    private Guid? CurrentUserId => Guid.TryParse(userManager.GetUserId(User), out var id) ? id : null;

    public async Task<IActionResult> Index(int? annee, Guid? groupeId, Guid? brancheId, StatutInscriptionAnnuelle? statut, string? importReportId = null)
    {
        var year = annee ?? DateTime.UtcNow.Year;
        var (page, ps) = ListPagination.Read(Request);

        var query = db.InscriptionsAnnuellesScouts.AsNoTracking()
            .Include(i => i.Scout)
            .Include(i => i.Groupe)
            .Include(i => i.Branche)
            .AsQueryable();

        query = query.Where(i => i.AnneeReference == year);

        if (groupeId.HasValue && groupeId.Value != Guid.Empty)
        {
            query = query.Where(i => i.GroupeId == groupeId.Value);
        }

        if (brancheId.HasValue && brancheId.Value != Guid.Empty)
        {
            query = query.Where(i => i.BrancheId == brancheId.Value);
        }

        if (statut.HasValue)
        {
            query = query.Where(i => i.Statut == statut.Value);
        }

        var total = await query.CountAsync();
        var (p, pageSize, skip, totalPages) = ListPagination.Normalize(page, ps, total);
        var inscriptions = await query
            .OrderBy(i => i.Scout.Nom)
            .ThenBy(i => i.Scout.Prenom)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Annee = year;
        ViewBag.SelectedGroupeId = groupeId;
        ViewBag.SelectedBrancheId = brancheId;
        ViewBag.SelectedStatut = statut;
        ViewBag.Groupes = await db.Groupes.Where(g => g.IsActive).OrderBy(g => g.Nom).ToListAsync();
        ViewBag.Branches = await db.Branches.Where(b => b.IsActive).OrderBy(b => b.Nom).ToListAsync();
        ViewBag.ImportError = TempData["ImportError"] as string;
        ViewBag.ImportReport = ResolveImportReport(importReportId);
        ListPagination.SetViewData(ViewData, HttpContext, p, pageSize, total, totalPages);
        return View(inscriptions);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Create(Guid? scoutId)
    {
        var model = new InscriptionAnnuelleScout
        {
            AnneeReference = DateTime.UtcNow.Year,
            LibelleAnnee = BuildYearLabel(DateTime.UtcNow.Year),
            DateInscription = DateTime.UtcNow.Date,
            ScoutId = scoutId ?? Guid.Empty
        };

        if (scoutId.HasValue && scoutId.Value != Guid.Empty)
        {
            var scout = await db.Scouts.FirstOrDefaultAsync(s => s.Id == scoutId.Value && s.IsActive);
            if (scout is not null)
            {
                ApplySnapshotFromScout(model, scout, overwrite: true);
                model.CotisationNationaleAjour = model.AnneeReference == DateTime.UtcNow.Year && scout.AssuranceAnnuelle;
            }
        }

        await LoadReferenceDataAsync(model.ScoutId, model.GroupeId, model.BrancheId);
        return View("Upsert", model);
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InscriptionAnnuelleScout model)
    {
        var scout = await ValidateModelAsync(model);
        if (scout is not null)
        {
            model.CotisationNationaleAjour = model.AnneeReference == DateTime.UtcNow.Year && scout.AssuranceAnnuelle;
        }

        if (!ModelState.IsValid || scout is null)
        {
            await LoadReferenceDataAsync(model.ScoutId, model.GroupeId, model.BrancheId);
            return View("Upsert", model);
        }

        model.Id = Guid.NewGuid();
        model.LibelleAnnee = NormalizeYearLabel(model.AnneeReference, model.LibelleAnnee);
        model.DateInscription = EnsureUtc(model.DateInscription == default ? DateTime.UtcNow : model.DateInscription);
        ApplySnapshotFromScout(model, scout, overwrite: false);

        if (model.Statut == StatutInscriptionAnnuelle.Validee)
        {
            model.DateValidation ??= DateTime.UtcNow;
            model.ValideParId ??= CurrentUserId;
        }

        db.InscriptionsAnnuellesScouts.Add(model);
        await db.SaveChangesAsync();
        TempData["Success"] = "Inscription annuelle enregistree.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var inscription = await db.InscriptionsAnnuellesScouts.AsNoTracking()
            .Include(i => i.Scout)
            .Include(i => i.Groupe)
            .Include(i => i.Branche)
            .Include(i => i.ValidePar)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (inscription is null) return NotFound();
        return View(inscription);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var inscription = await db.InscriptionsAnnuellesScouts.FirstOrDefaultAsync(i => i.Id == id);
        if (inscription is null) return NotFound();
        await LoadReferenceDataAsync(inscription.ScoutId, inscription.GroupeId, inscription.BrancheId);
        return View("Upsert", inscription);
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, InscriptionAnnuelleScout model)
    {
        var inscription = await db.InscriptionsAnnuellesScouts.FirstOrDefaultAsync(i => i.Id == id);
        if (inscription is null) return NotFound();

        var scout = await ValidateModelAsync(model, id);
        model.CotisationNationaleAjour = inscription.CotisationNationaleAjour;
        if (!ModelState.IsValid || scout is null)
        {
            model.Id = id;
            await LoadReferenceDataAsync(model.ScoutId, model.GroupeId, model.BrancheId);
            return View("Upsert", model);
        }

        inscription.ScoutId = model.ScoutId;
        inscription.AnneeReference = model.AnneeReference;
        inscription.LibelleAnnee = NormalizeYearLabel(model.AnneeReference, model.LibelleAnnee);
        inscription.DateInscription = EnsureUtc(model.DateInscription == default ? inscription.DateInscription : model.DateInscription);
        inscription.Statut = model.Statut;
        inscription.InscriptionParoissialeValidee = model.InscriptionParoissialeValidee;
        inscription.Observations = model.Observations?.Trim();
        inscription.GroupeId = model.GroupeId;
        inscription.BrancheId = model.BrancheId;
        inscription.FonctionSnapshot = string.IsNullOrWhiteSpace(model.FonctionSnapshot) ? null : model.FonctionSnapshot.Trim();
        ApplySnapshotFromScout(inscription, scout, overwrite: false);

        if (inscription.Statut == StatutInscriptionAnnuelle.Validee)
        {
            inscription.DateValidation = model.DateValidation ?? inscription.DateValidation ?? DateTime.UtcNow;
            inscription.ValideParId = model.ValideParId ?? inscription.ValideParId ?? CurrentUserId;
        }
        else if (inscription.Statut == StatutInscriptionAnnuelle.Suspendue)
        {
            inscription.DateValidation = null;
            inscription.ValideParId = null;
        }

        await db.SaveChangesAsync();
        TempData["Success"] = "Inscription annuelle mise a jour.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Valider(Guid id)
    {
        var inscription = await db.InscriptionsAnnuellesScouts.FirstOrDefaultAsync(i => i.Id == id);
        if (inscription is null) return NotFound();

        inscription.Statut = StatutInscriptionAnnuelle.Validee;
        inscription.DateValidation = DateTime.UtcNow;
        inscription.ValideParId = CurrentUserId;
        await db.SaveChangesAsync();

        TempData["Success"] = "Inscription annuelle validee.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public IActionResult DownloadImportTemplate()
    {
        var content = GenerateImportTemplate();
        return File(
            content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"modele-import-inscriptions-annuelles-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportExcel(IFormFile? fichier, int anneeReference)
    {
        if (anneeReference < 2000 || anneeReference > 2100)
        {
            TempData["ImportError"] = "L'annee de reference est invalide.";
            return RedirectToAction(nameof(Index));
        }

        if (fichier is null || fichier.Length == 0)
        {
            TempData["ImportError"] = "Veuillez selectionner un fichier Excel.";
            return RedirectToAction(nameof(Index), new { annee = anneeReference });
        }

        var extension = Path.GetExtension(fichier.FileName);
        if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            TempData["ImportError"] = "Le fichier doit etre au format .xlsx.";
            return RedirectToAction(nameof(Index), new { annee = anneeReference });
        }

        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(fichier.OpenReadStream());
        }
        catch
        {
            TempData["ImportError"] = "Le fichier selectionne n'est pas un classeur Excel (.xlsx) valide.";
            return RedirectToAction(nameof(Index), new { annee = anneeReference });
        }

        var report = new InscriptionAnnuelleImportResultDto();

        using (workbook)
        {
            IXLWorksheet worksheet;
            try
            {
                worksheet = workbook.Worksheets.First();
            }
            catch
            {
                TempData["ImportError"] = "Le fichier Excel ne contient aucune feuille exploitable.";
                return RedirectToAction(nameof(Index), new { annee = anneeReference });
            }

            var headerMap = worksheet.Row(1)
                .CellsUsed()
                .ToDictionary(
                    cell => NormalizeHeader(cell.GetString()),
                    cell => cell.Address.ColumnNumber,
                    StringComparer.OrdinalIgnoreCase);

            var activeScouts = await db.Scouts
                .AsNoTracking()
                .Include(s => s.Groupe)
                .Include(s => s.Branche)
                .Where(s => s.IsActive)
                .ToListAsync();

            var scoutsByMatricule = activeScouts
                .Where(s => !string.IsNullOrWhiteSpace(s.Matricule))
                .GroupBy(s => NormalizeLookup(s.Matricule))
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var scoutsByIdentity = activeScouts
                .GroupBy(s => BuildScoutIdentityKey(s.Nom, s.Prenom, s.DateNaissance))
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var activeGroupes = await db.Groupes
                .Where(g => g.IsActive)
                .ToListAsync();
            var groupesByName = activeGroupes
                .GroupBy(g => NormalizeLookup(g.Nom))
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var groupesById = activeGroupes.ToDictionary(g => g.Id);

            var activeBranches = await db.Branches
                .Where(b => b.IsActive)
                .ToListAsync();
            var branchesByName = activeBranches.ToLookup(b => NormalizeLookup(b.Nom), StringComparer.OrdinalIgnoreCase);

            var inscriptions = await db.InscriptionsAnnuellesScouts
                .Where(i => i.AnneeReference == anneeReference)
                .ToListAsync();
            var inscriptionsByScoutId = inscriptions.ToDictionary(i => i.ScoutId);
            var processedScoutIds = new HashSet<Guid>();
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
            {
                var row = worksheet.Row(rowNumber);
                if (row.CellsUsed().All(c => string.IsNullOrWhiteSpace(c.GetString())))
                {
                    continue;
                }

                var matricule = ScoutMatriculeFormat.NormalizeOptional(ReadString(row, headerMap, "matricule"));
                var nom = ReadString(row, headerMap, "nom", "nomscout");
                var prenom = ReadString(row, headerMap, "prenom", "prenomscout");
                var dateNaissance = ReadOptionalDate(row, headerMap, "datenaissance", "date_naissance", "dateofbirth");
                var groupeNom = ReadString(row, headerMap, "groupe", "entite");
                var brancheNom = ReadString(row, headerMap, "branche");
                var fonction = ReadString(row, headerMap, "fonction");
                var statutRaw = ReadString(row, headerMap, "statut");
                var observations = ReadString(row, headerMap, "observations", "observation", "commentaire");
                var dateInscription = ReadOptionalDate(row, headerMap, "dateinscription", "date_inscription");
                var dateValidation = ReadOptionalDate(row, headerMap, "datevalidation", "date_validation");
                var scoutLabel = BuildScoutLabel(matricule, nom, prenom);
                var errors = new List<string>();

                Scout? scout = null;
                if (!string.IsNullOrWhiteSpace(matricule))
                {
                    var matriculeKey = NormalizeLookup(matricule);
                    if (!scoutsByMatricule.TryGetValue(matriculeKey, out scout))
                    {
                        errors.Add("Aucun scout actif ne correspond a ce matricule.");
                    }
                }
                else
                {
                    var identityKey = BuildScoutIdentityKeyOrDefault(nom, prenom, dateNaissance);
                    if (string.IsNullOrWhiteSpace(identityKey))
                    {
                        errors.Add("Renseignez soit le matricule, soit Nom, Prenom et DateNaissance pour rapprocher un scout existant.");
                    }
                    else if (!scoutsByIdentity.TryGetValue(identityKey, out var matches) || matches.Count == 0)
                    {
                        errors.Add("Aucun scout actif ne correspond a l'identite fournie.");
                    }
                    else if (matches.Count > 1)
                    {
                        errors.Add("Plusieurs scouts actifs correspondent a l'identite fournie. Le rapprochement doit etre verifie manuellement.");
                    }
                    else
                    {
                        scout = matches[0];
                    }
                }

                if (scout is not null)
                {
                    if (!processedScoutIds.Add(scout.Id))
                    {
                        errors.Add("Le meme scout apparait plusieurs fois dans ce fichier pour l'annee importee.");
                    }

                    if (HasIdentityMismatch(nom, prenom, dateNaissance, scout))
                    {
                        errors.Add("Les informations d'identite du fichier ne correspondent pas a la fiche scout.");
                    }
                }

                Groupe? groupe = null;
                if (!string.IsNullOrWhiteSpace(groupeNom))
                {
                    if (!groupesByName.TryGetValue(NormalizeLookup(groupeNom), out groupe))
                    {
                        errors.Add("Le groupe renseigne est introuvable ou inactif.");
                    }
                }

                Branche? branche = null;
                if (!string.IsNullOrWhiteSpace(brancheNom))
                {
                    var candidates = branchesByName[NormalizeLookup(brancheNom)].ToList();
                    if (candidates.Count == 0)
                    {
                        errors.Add("La branche renseignee est introuvable ou inactive.");
                    }
                    else
                    {
                        if (groupe is not null)
                        {
                            candidates = candidates.Where(b => b.GroupeId == groupe.Id).ToList();
                        }
                        else if (scout?.GroupeId is Guid scoutGroupeId)
                        {
                            var scoutCandidates = candidates.Where(b => b.GroupeId == scoutGroupeId).ToList();
                            if (scoutCandidates.Count == 1)
                            {
                                candidates = scoutCandidates;
                            }
                        }

                        if (candidates.Count == 1)
                        {
                            branche = candidates[0];
                        }
                        else if (candidates.Count > 1)
                        {
                            errors.Add("La branche renseignee existe dans plusieurs groupes. Renseignez aussi le groupe dans le fichier.");
                        }
                        else
                        {
                            errors.Add("La branche renseignee n'appartient pas au groupe selectionne dans le fichier.");
                        }
                    }
                }

                if (branche is not null)
                {
                    if (groupe is null)
                    {
                        groupe = groupesById.GetValueOrDefault(branche.GroupeId);
                    }
                    else if (branche.GroupeId != groupe.Id)
                    {
                        errors.Add("La branche renseignee doit appartenir au groupe renseigne.");
                    }
                }

                if (!TryResolveStatut(statutRaw, out var statut))
                {
                    errors.Add("Le statut importe est invalide. Valeurs attendues : Enregistree, Validee ou Suspendue.");
                }

                if (!TryReadOptionalBool(row, headerMap, out var inscriptionValidee, out var boolError, "inscriptionparoissialevalidee", "validationparoissiale", "inscription_validee"))
                {
                    errors.Add(boolError);
                }

                if (statut == StatutInscriptionAnnuelle.Validee && CurrentUserId is null)
                {
                    errors.Add("Impossible de journaliser la validation importee sans utilisateur connecte.");
                }

                if (errors.Count != 0)
                {
                    report.SkippedCount++;
                    report.Errors.Add(new InscriptionAnnuelleImportErrorDto
                    {
                        LineNumber = rowNumber,
                        ScoutLabel = scoutLabel,
                        Message = string.Join(" ", errors)
                    });
                    continue;
                }

                var inscriptionExiste = inscriptionsByScoutId.TryGetValue(scout!.Id, out var inscription);
                if (!inscriptionExiste)
                {
                    inscription = new InscriptionAnnuelleScout
                    {
                        Id = Guid.NewGuid(),
                        ScoutId = scout.Id,
                        AnneeReference = anneeReference,
                        LibelleAnnee = BuildYearLabel(anneeReference),
                        DateInscription = EnsureUtc(dateInscription ?? DateTime.UtcNow),
                        CotisationNationaleAjour = anneeReference == DateTime.UtcNow.Year && scout.AssuranceAnnuelle
                    };
                    db.InscriptionsAnnuellesScouts.Add(inscription);
                    inscriptionsByScoutId[scout.Id] = inscription;
                }

                inscription!.ScoutId = scout.Id;
                inscription.AnneeReference = anneeReference;
                inscription.LibelleAnnee = BuildYearLabel(anneeReference);
                inscription.DateInscription = EnsureUtc(dateInscription ?? (inscription.DateInscription == default ? DateTime.UtcNow : inscription.DateInscription));
                inscription.Statut = statut;
                inscription.InscriptionParoissialeValidee = inscriptionValidee ?? inscription.InscriptionParoissialeValidee;
                inscription.Observations = string.IsNullOrWhiteSpace(observations) ? inscription.Observations : observations.Trim();
                inscription.GroupeId = groupe?.Id ?? inscription.GroupeId ?? scout.GroupeId;
                inscription.BrancheId = branche?.Id ?? inscription.BrancheId ?? scout.BrancheId;
                inscription.FonctionSnapshot = !string.IsNullOrWhiteSpace(fonction)
                    ? fonction.Trim()
                    : string.IsNullOrWhiteSpace(inscription.FonctionSnapshot)
                        ? string.IsNullOrWhiteSpace(scout.Fonction) ? null : scout.Fonction.Trim()
                        : inscription.FonctionSnapshot;
                ApplySnapshotFromScout(inscription, scout, overwrite: false);

                if (inscription.Statut == StatutInscriptionAnnuelle.Validee)
                {
                    inscription.DateValidation = EnsureUtc(dateValidation ?? inscription.DateValidation ?? DateTime.UtcNow);
                    inscription.ValideParId = CurrentUserId ?? inscription.ValideParId;
                }
                else
                {
                    inscription.DateValidation = null;
                    inscription.ValideParId = null;
                }

                var entryLabel = BuildImportEntryLabel(scout, anneeReference);
                if (inscriptionExiste)
                {
                    report.UpdatedCount++;
                    report.UpdatedEntries.Add(entryLabel);
                }
                else
                {
                    report.CreatedCount++;
                    report.CreatedEntries.Add(entryLabel);
                }
            }
        }

        await db.SaveChangesAsync();
        var importReportId = StoreImportReport(report);
        return RedirectToAction(nameof(Index), new { annee = anneeReference, importReportId });
    }

    private async Task<Scout?> ValidateModelAsync(InscriptionAnnuelleScout model, Guid? currentId = null)
    {
        var scout = await db.Scouts.FirstOrDefaultAsync(s => s.Id == model.ScoutId && s.IsActive);
        if (scout is null)
        {
            ModelState.AddModelError(nameof(model.ScoutId), "Le scout selectionne est introuvable ou inactif.");
            return null;
        }

        if (await db.InscriptionsAnnuellesScouts.AnyAsync(i => i.Id != currentId && i.ScoutId == model.ScoutId && i.AnneeReference == model.AnneeReference))
        {
            ModelState.AddModelError(nameof(model.AnneeReference), "Une inscription annuelle existe deja pour ce scout sur cette annee.");
        }

        if (model.AnneeReference < 2000 || model.AnneeReference > 2100)
        {
            ModelState.AddModelError(nameof(model.AnneeReference), "L'annee de reference est invalide.");
        }

        if (model.GroupeId.HasValue)
        {
            var groupeExiste = await db.Groupes.AnyAsync(g => g.Id == model.GroupeId.Value && g.IsActive);
            if (!groupeExiste)
            {
                ModelState.AddModelError(nameof(model.GroupeId), "Le groupe selectionne est introuvable ou inactif.");
            }
        }

        if (model.BrancheId.HasValue)
        {
            var branche = await db.Branches.Where(b => b.Id == model.BrancheId.Value && b.IsActive)
                .Select(b => new { b.GroupeId })
                .FirstOrDefaultAsync();
            if (branche is null)
            {
                ModelState.AddModelError(nameof(model.BrancheId), "La branche selectionnee est introuvable ou inactive.");
            }
            else if (model.GroupeId.HasValue && branche.GroupeId != model.GroupeId.Value)
            {
                ModelState.AddModelError(nameof(model.BrancheId), "La branche selectionnee doit appartenir au groupe selectionne.");
            }
        }

        return scout;
    }

    private async Task LoadReferenceDataAsync(Guid? selectedScoutId, Guid? selectedGroupeId, Guid? selectedBrancheId)
    {
        ViewBag.Scouts = await db.Scouts
            .Include(s => s.Groupe)
            .Include(s => s.Branche)
            .Where(s => s.IsActive)
            .OrderBy(s => s.Nom)
            .ThenBy(s => s.Prenom)
            .ToListAsync();
        ViewBag.Groupes = await db.Groupes
            .Where(g => g.IsActive)
            .OrderBy(g => g.Nom)
            .ToListAsync();
        ViewBag.Branches = await db.Branches
            .Where(b => b.IsActive)
            .OrderBy(b => b.Nom)
            .ToListAsync();
        ViewBag.SelectedScoutId = selectedScoutId;
        ViewBag.SelectedGroupeId = selectedGroupeId;
        ViewBag.SelectedBrancheId = selectedBrancheId;
    }

    private static void ApplySnapshotFromScout(InscriptionAnnuelleScout inscription, Scout scout, bool overwrite)
    {
        if (overwrite || !inscription.GroupeId.HasValue)
        {
            inscription.GroupeId = scout.GroupeId;
        }

        if (overwrite || !inscription.BrancheId.HasValue)
        {
            inscription.BrancheId = scout.BrancheId;
        }

        if (overwrite || string.IsNullOrWhiteSpace(inscription.FonctionSnapshot))
        {
            inscription.FonctionSnapshot = string.IsNullOrWhiteSpace(scout.Fonction) ? null : scout.Fonction.Trim();
        }
    }

    private string StoreImportReport(InscriptionAnnuelleImportResultDto result)
    {
        var reportId = Guid.NewGuid().ToString("N");
        memoryCache.Set($"{ImportReportCachePrefix}{reportId}", result, ImportReportLifetime);
        return reportId;
    }

    private InscriptionAnnuelleImportResultDto? ResolveImportReport(string? importReportId)
    {
        if (string.IsNullOrWhiteSpace(importReportId))
        {
            return null;
        }

        return memoryCache.TryGetValue($"{ImportReportCachePrefix}{importReportId}", out InscriptionAnnuelleImportResultDto? report)
            ? report
            : null;
    }

    private static bool TryFindColumn(IDictionary<string, int> headerMap, out int column, params string[] headers)
    {
        foreach (var header in headers)
        {
            if (headerMap.TryGetValue(header, out column))
            {
                return true;
            }
        }

        column = 0;
        return false;
    }

    private static string ReadString(IXLRow row, IDictionary<string, int> headerMap, params string[] headers)
    {
        return TryFindColumn(headerMap, out var column, headers)
            ? row.Cell(column).GetString().Trim()
            : string.Empty;
    }

    private static DateTime? ReadOptionalDate(IXLRow row, IDictionary<string, int> headerMap, params string[] headers)
    {
        if (!TryFindColumn(headerMap, out var column, headers))
        {
            return null;
        }

        var cell = row.Cell(column);
        if (cell.IsEmpty())
        {
            return null;
        }

        if (cell.DataType == XLDataType.DateTime)
        {
            return cell.GetDateTime().Date;
        }

        if (cell.DataType == XLDataType.Number)
        {
            return DateTime.FromOADate(cell.GetDouble()).Date;
        }

        var raw = cell.GetString().Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return DateTime.TryParse(raw, CultureInfo.GetCultureInfo("fr-FR"), DateTimeStyles.None, out var parsed)
            || DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed)
            ? parsed.Date
            : null;
    }

    private static bool TryReadOptionalBool(IXLRow row, IDictionary<string, int> headerMap, out bool? value, out string errorMessage, params string[] headers)
    {
        value = null;
        errorMessage = string.Empty;

        if (!TryFindColumn(headerMap, out var column, headers))
        {
            return true;
        }

        var raw = row.Cell(column).GetString().Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        switch (NormalizeLookup(raw))
        {
            case "oui":
            case "o":
            case "true":
            case "1":
            case "valide":
                value = true;
                return true;
            case "non":
            case "n":
            case "false":
            case "0":
                value = false;
                return true;
            default:
                errorMessage = "La valeur de validation paroissiale est invalide. Utilisez Oui ou Non.";
                return false;
        }
    }

    private static bool TryResolveStatut(string? raw, out StatutInscriptionAnnuelle statut)
    {
        statut = StatutInscriptionAnnuelle.Enregistree;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        return NormalizeLookup(raw) switch
        {
            "enregistree" => true,
            "validee" => (statut = StatutInscriptionAnnuelle.Validee) == StatutInscriptionAnnuelle.Validee,
            "suspendue" => (statut = StatutInscriptionAnnuelle.Suspendue) == StatutInscriptionAnnuelle.Suspendue,
            _ => false
        };
    }

    private static bool HasIdentityMismatch(string? nom, string? prenom, DateTime? dateNaissance, Scout scout)
    {
        var nomMismatch = !string.IsNullOrWhiteSpace(nom)
            && !string.Equals(NormalizeLookup(nom), NormalizeLookup(scout.Nom), StringComparison.Ordinal);
        var prenomMismatch = !string.IsNullOrWhiteSpace(prenom)
            && !string.Equals(NormalizeLookup(prenom), NormalizeLookup(scout.Prenom), StringComparison.Ordinal);
        var dateMismatch = dateNaissance.HasValue && dateNaissance.Value.Date != scout.DateNaissance.Date;
        return nomMismatch || prenomMismatch || dateMismatch;
    }

    private static string NormalizeHeader(string value) => DatabaseText.NormalizeSearchKey(value);

    private static string NormalizeLookup(string? value) => DatabaseText.NormalizeSearchKey(value);

    private static string BuildScoutIdentityKey(string nom, string prenom, DateTime dateNaissance) =>
        $"{NormalizeLookup(nom)}|{NormalizeLookup(prenom)}|{dateNaissance:yyyyMMdd}";

    private static string? BuildScoutIdentityKeyOrDefault(string? nom, string? prenom, DateTime? dateNaissance)
    {
        if (string.IsNullOrWhiteSpace(nom) || string.IsNullOrWhiteSpace(prenom) || !dateNaissance.HasValue)
        {
            return null;
        }

        return BuildScoutIdentityKey(nom, prenom, dateNaissance.Value);
    }

    private static string BuildScoutLabel(string? matricule, string? nom, string? prenom)
    {
        if (!string.IsNullOrWhiteSpace(matricule))
        {
            return matricule.Trim();
        }

        return string.Join(" ", new[] { prenom?.Trim(), nom?.Trim() }.Where(value => !string.IsNullOrWhiteSpace(value))).Trim();
    }

    private static string BuildImportEntryLabel(Scout scout, int year)
    {
        return $"{scout.Prenom} {scout.Nom} - {BuildYearLabel(year)}".Trim();
    }

    private static string BuildYearLabel(int year) => $"{year}-{year + 1}";

    private static string NormalizeYearLabel(int year, string? currentValue)
    {
        return string.IsNullOrWhiteSpace(currentValue) ? BuildYearLabel(year) : currentValue.Trim();
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private static byte[] GenerateImportTemplate()
    {
        using var workbook = new XLWorkbook();
        var data = workbook.Worksheets.Add("InscriptionsAnnuelles");
        var headers = new[]
        {
            "Matricule",
            "Nom",
            "Prenom",
            "DateNaissance",
            "Groupe",
            "Branche",
            "Fonction",
            "Statut",
            "InscriptionParoissialeValidee",
            "DateInscription",
            "DateValidation",
            "Observations"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            data.Cell(1, i + 1).Value = headers[i];
            data.Cell(1, i + 1).Style.Font.Bold = true;
            data.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#EAF4D7");
        }

        data.Cell(2, 1).Value = "0514706Z";
        data.Cell(2, 2).Value = "YOH";
        data.Cell(2, 3).Value = "Hermann";
        data.Cell(2, 4).Value = new DateTime(1990, 1, 15);
        data.Cell(2, 5).Value = "Equipe de District Mango Taika";
        data.Cell(2, 6).Value = "LES BENEVOLES - Adultes Dans le Scoutisme (ADS)";
        data.Cell(2, 7).Value = "COMMISSAIRE DE DISTRICT (CD)";
        data.Cell(2, 8).Value = "Validee";
        data.Cell(2, 9).Value = "Oui";
        data.Cell(2, 10).Value = DateTime.UtcNow.Date;
        data.Cell(2, 11).Value = DateTime.UtcNow.Date;
        data.Cell(2, 12).Value = "Import initial.";
        data.Cell(2, 4).Style.DateFormat.Format = "dd/MM/yyyy";
        data.Cell(2, 10).Style.DateFormat.Format = "dd/MM/yyyy";
        data.Cell(2, 11).Style.DateFormat.Format = "dd/MM/yyyy";
        data.Columns().AdjustToContents();
        data.SheetView.FreezeRows(1);

        var guide = workbook.Worksheets.Add("Guide");
        guide.Cell("A1").Value = "Principe";
        guide.Cell("A2").Value = "L'import cree ou met a jour les inscriptions annuelles pour l'annee choisie dans la fenetre d'import.";
        guide.Cell("A4").Value = "Colonnes de rapprochement";
        guide.Cell("A5").Value = "Matricule";
        guide.Cell("A6").Value = "ou, si le matricule est vide : Nom + Prenom + DateNaissance";
        guide.Cell("A8").Value = "Colonnes metier";
        guide.Cell("A9").Value = "Groupe, Branche et Fonction sont optionnels. Si elles sont vides, le snapshot actuel du scout est repris.";
        guide.Cell("A10").Value = "Statut accepte : Enregistree, Validee, Suspendue.";
        guide.Cell("A11").Value = "InscriptionParoissialeValidee accepte : Oui/Non, True/False, 1/0.";
        guide.Cell("A13").Value = "Regles";
        guide.Cell("A14").Value = "Ce fichier ne cree pas de nouveaux scouts.";
        guide.Cell("A15").Value = "La cotisation nationale n'est pas geree par cet import.";
        guide.Cell("A16").Value = "Une meme ligne scout ne doit apparaitre qu'une seule fois par fichier et par annee d'import.";
        guide.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
