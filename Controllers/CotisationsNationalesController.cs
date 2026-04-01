using ClosedXML.Excel;
using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
using MangoTaika.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace MangoTaika.Controllers;

[Authorize(Roles = "Administrateur,Gestionnaire,Superviseur,Consultant")]
public class CotisationsNationalesController(AppDbContext db, UserManager<ApplicationUser> userManager) : Controller
{
    private Guid? CurrentUserId => Guid.TryParse(userManager.GetUserId(User), out var id) ? id : null;

    public async Task<IActionResult> Index(int? annee, Guid? groupeId, Guid? brancheId, StatutLigneCotisationNationale? statut, Guid? importId)
    {
        var year = annee ?? DateTime.UtcNow.Year;
        var (page, ps) = ListPagination.Read(Request);

        var imports = await db.CotisationsNationalesImports.AsNoTracking()
            .OrderByDescending(i => i.AnneeReference)
            .ThenByDescending(i => i.DateImport)
            .ToListAsync();

        var selectedImport = ResolveSelectedImport(importId, imports, year);

        var query = db.CotisationsNationalesImportLignes.AsNoTracking()
            .Include(l => l.Import)
            .Include(l => l.Scout).ThenInclude(s => s!.Groupe)
            .Include(l => l.Scout).ThenInclude(s => s!.Branche)
            .Where(l => l.Import.AnneeReference == year);

        query = selectedImport is not null
            ? query.Where(l => l.ImportId == selectedImport.Id)
            : query.Where(l => false);

        if (groupeId.HasValue && groupeId.Value != Guid.Empty)
        {
            query = query.Where(l => l.Scout != null && l.Scout.GroupeId == groupeId.Value);
        }

        if (brancheId.HasValue && brancheId.Value != Guid.Empty)
        {
            query = query.Where(l => l.Scout != null && l.Scout.BrancheId == brancheId.Value);
        }

        if (statut.HasValue)
        {
            query = query.Where(l => l.Statut == statut.Value);
        }

        var totalCount = await query.CountAsync();
        var (p, pageSize, skip, totalPages) = ListPagination.Normalize(page, ps, totalCount);
        var lignes = await query
            .OrderBy(l => l.Statut)
            .ThenBy(l => l.Matricule)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        var model = new CotisationsNationalesIndexViewModel
        {
            AnneeReference = year,
            GroupeId = groupeId,
            BrancheId = brancheId,
            Statut = statut,
            ImportId = selectedImport?.Id,
            ImportSelectionne = selectedImport,
            Lignes = lignes,
            Groupes = await db.Groupes
                .Where(g => g.IsActive)
                .OrderBy(g => g.Nom)
                .Select(g => new SelectListItem
                {
                    Value = g.Id.ToString(),
                    Text = g.Nom,
                    Selected = groupeId == g.Id
                })
                .ToListAsync(),
            Branches = await db.Branches
                .Where(b => b.IsActive)
                .OrderBy(b => b.Nom)
                .Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text = b.Nom,
                    Selected = brancheId == b.Id
                })
                .ToListAsync(),
            Imports = imports
                .Where(i => i.AnneeReference == year)
                .Select(i => new SelectListItem
                {
                    Value = i.Id.ToString(),
                    Text = $"{i.DateImport:dd/MM/yyyy HH:mm} - {i.NomFichier}",
                    Selected = selectedImport is not null && i.Id == selectedImport.Id
                })
                .ToList(),
            NombreVisible = totalCount,
            NombreAjour = selectedImport?.NombreAjour ?? 0,
            NombreNonAjour = selectedImport?.NombreNonAjour ?? 0,
            NombreAVerifier = selectedImport?.NombreAVerifier ?? 0
        };

        ListPagination.SetViewData(ViewData, HttpContext, p, pageSize, totalCount, totalPages);
        return View(model);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var import = await db.CotisationsNationalesImports.AsNoTracking()
            .Include(i => i.Createur)
            .Include(i => i.Lignes)
                .ThenInclude(l => l.Scout)
                    .ThenInclude(s => s!.Groupe)
            .Include(i => i.Lignes)
                .ThenInclude(l => l.Scout)
                    .ThenInclude(s => s!.Branche)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (import is null)
        {
            return NotFound();
        }

        return View(import);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public IActionResult Template()
    {
        var content = GenerateTemplate();
        return File(
            content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "modele-cotisations-nationales.xlsx");
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Importer(IFormFile fichier, int anneeReference)
    {
        if (CurrentUserId is null)
        {
            return Challenge();
        }

        if (anneeReference < 2000 || anneeReference > 2100)
        {
            TempData["Error"] = "L'annee de reference est invalide.";
            return RedirectToAction(nameof(Index));
        }

        if (fichier is null || fichier.Length == 0)
        {
            TempData["Error"] = "Selectionnez un fichier Excel (.xlsx) a importer.";
            return RedirectToAction(nameof(Index), new { annee = anneeReference });
        }

        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(fichier.OpenReadStream());
        }
        catch
        {
            TempData["Error"] = "Le fichier selectionne n'est pas un classeur Excel (.xlsx) valide.";
            return RedirectToAction(nameof(Index), new { annee = anneeReference });
        }

        using (workbook)
        {
            IXLWorksheet worksheet;
            try
            {
                worksheet = workbook.Worksheets.First();
            }
            catch
            {
                TempData["Error"] = "Le fichier Excel ne contient aucune feuille exploitable.";
                return RedirectToAction(nameof(Index), new { annee = anneeReference });
            }

            var headerMap = worksheet.Row(1)
                .CellsUsed()
                .ToDictionary(
                    cell => NormalizeHeader(cell.GetString()),
                    cell => cell.Address.ColumnNumber,
                    StringComparer.OrdinalIgnoreCase);

            if (!headerMap.ContainsKey("matricule"))
            {
                TempData["Error"] = "La colonne Matricule est obligatoire pour le rapprochement national.";
                return RedirectToAction(nameof(Index), new { annee = anneeReference });
            }

            var activeScouts = await db.Scouts
                .Include(s => s.Groupe)
                .Include(s => s.Branche)
                .Where(s => s.IsActive)
                .ToListAsync();

            var scoutsByMatricule = activeScouts
                .Where(s => !string.IsNullOrWhiteSpace(s.Matricule))
                .GroupBy(s => NormalizeLookup(s.Matricule))
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var inscriptions = await db.InscriptionsAnnuellesScouts
                .Where(i => i.AnneeReference == anneeReference)
                .ToListAsync();
            var inscriptionsByScoutId = inscriptions
                .GroupBy(i => i.ScoutId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(i => i.DateInscription).First());

            var referencePrefix = BuildTransactionReferencePrefix(anneeReference);
            var transactions = await db.TransactionsFinancieres
                .Where(t => !t.EstSupprime
                    && t.Categorie == CategorieFinance.Cotisation
                    && t.ScoutId != null
                    && t.Reference != null
                    && t.Reference.StartsWith(referencePrefix))
                .ToListAsync();
            var transactionsByScoutId = transactions
                .Where(t => t.ScoutId.HasValue)
                .GroupBy(t => t.ScoutId!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            var import = new CotisationNationaleImport
            {
                Id = Guid.NewGuid(),
                AnneeReference = anneeReference,
                NomFichier = Path.GetFileName(fichier.FileName),
                DateImport = DateTime.UtcNow,
                CreateurId = CurrentUserId.Value
            };
            db.CotisationsNationalesImports.Add(import);

            var lignes = new List<CotisationNationaleImportLigne>();
            var seenMatricules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var recognizedScoutIds = new HashSet<Guid>();
            var totalMontant = 0m;
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
            {
                var row = worksheet.Row(rowNumber);
                if (row.CellsUsed().All(c => string.IsNullOrWhiteSpace(c.GetString())))
                {
                    continue;
                }

                var matricule = ScoutMatriculeFormat.Normalize(ReadString(row, headerMap, "matricule"));
                var nomImporte = ReadString(row, headerMap, "nom", "nomscout");
                var prenomImporte = ReadString(row, headerMap, "prenom", "prenomscout");
                var nomComplet = string.Join(" ", new[] { prenomImporte, nomImporte }.Where(v => !string.IsNullOrWhiteSpace(v))).Trim();
                var lookupKey = NormalizeLookup(matricule);
                var raisons = new List<string>();

                if (!TryReadDecimal(row, headerMap, out var montant, out var montantErreur, "montant", "montantfcfa", "montantcotisation"))
                {
                    raisons.Add(montantErreur);
                }

                if (string.IsNullOrWhiteSpace(matricule))
                {
                    raisons.Add("Matricule absent.");
                }
                else if (!ScoutMatriculeFormat.IsValid(matricule))
                {
                    raisons.Add($"Matricule invalide: {matricule}. Format attendu: {ScoutMatriculeFormat.Example}.");
                }

                Scout? scout = null;
                if (!string.IsNullOrWhiteSpace(lookupKey) && scoutsByMatricule.TryGetValue(lookupKey, out var matchedScout))
                {
                    scout = matchedScout;
                }
                else if (!string.IsNullOrWhiteSpace(matricule))
                {
                    raisons.Add("Aucun scout actif ne correspond a ce matricule.");
                }

                if (!string.IsNullOrWhiteSpace(lookupKey) && !seenMatricules.Add(lookupKey))
                {
                    raisons.Add("Matricule duplique dans le fichier national.");
                }

                if (scout is not null)
                {
                    recognizedScoutIds.Add(scout.Id);

                    if (HasIdentityMismatch(nomImporte, prenomImporte, scout))
                    {
                        raisons.Add("Le nom ou le prenom importe ne correspond pas a la fiche scout.");
                    }
                }

                if (montant.HasValue && montant.Value < 0)
                {
                    raisons.Add("Le montant ne peut pas etre negatif.");
                }

                var ligne = new CotisationNationaleImportLigne
                {
                    Id = Guid.NewGuid(),
                    ImportId = import.Id,
                    Import = import,
                    ScoutId = scout?.Id,
                    Matricule = matricule,
                    NomImporte = string.IsNullOrWhiteSpace(nomComplet)
                        ? (scout is null ? null : $"{scout.Prenom} {scout.Nom}".Trim())
                        : nomComplet,
                    Montant = montant,
                    Statut = raisons.Count == 0 && scout is not null
                        ? StatutLigneCotisationNationale.Ajour
                        : StatutLigneCotisationNationale.AVerifier,
                    Motif = raisons.Count == 0 ? null : string.Join(" ", raisons)
                };
                lignes.Add(ligne);
                db.CotisationsNationalesImportLignes.Add(ligne);

                if (scout is null)
                {
                    continue;
                }

                var inscription = EnsureInscription(inscriptionsByScoutId, scout, anneeReference);
                if (ligne.Statut == StatutLigneCotisationNationale.Ajour)
                {
                    inscription.CotisationNationaleAjour = true;
                    inscription.Observations = AppendObservation(inscription.Observations, $"Cotisation nationale rapprochee le {DateTime.UtcNow:dd/MM/yyyy}.");
                    SyncScoutCurrentCotisation(scout, anneeReference, true);

                    if (montant.HasValue && montant.Value > 0)
                    {
                        totalMontant += montant.Value;
                        UpsertCotisationTransaction(transactionsByScoutId, scout, anneeReference, montant.Value, CurrentUserId.Value);
                    }
                }
                else
                {
                    inscription.CotisationNationaleAjour = false;
                    inscription.Observations = AppendObservation(inscription.Observations, ligne.Motif);
                    SyncScoutCurrentCotisation(scout, anneeReference, false);
                }
            }

            foreach (var scout in activeScouts.Where(s => !recognizedScoutIds.Contains(s.Id)))
            {
                var inscription = EnsureInscription(inscriptionsByScoutId, scout, anneeReference);
                inscription.CotisationNationaleAjour = false;
                inscription.Observations = AppendObservation(inscription.Observations, $"Absent du fichier national importe le {DateTime.UtcNow:dd/MM/yyyy}.");
                SyncScoutCurrentCotisation(scout, anneeReference, false);

                var ligne = new CotisationNationaleImportLigne
                {
                    Id = Guid.NewGuid(),
                    ImportId = import.Id,
                    Import = import,
                    ScoutId = scout.Id,
                    Matricule = scout.Matricule,
                    NomImporte = $"{scout.Prenom} {scout.Nom}".Trim(),
                    Statut = StatutLigneCotisationNationale.NonAjour,
                    Motif = "Absent du fichier national.",
                    Montant = null
                };
                lignes.Add(ligne);
                db.CotisationsNationalesImportLignes.Add(ligne);
            }

            import.MontantTotal = totalMontant;
            import.NombreAjour = lignes.Count(l => l.Statut == StatutLigneCotisationNationale.Ajour);
            import.NombreNonAjour = lignes.Count(l => l.Statut == StatutLigneCotisationNationale.NonAjour);
            import.NombreAVerifier = lignes.Count(l => l.Statut == StatutLigneCotisationNationale.AVerifier);

            await db.SaveChangesAsync();
            TempData["Success"] = $"Import termine : {import.NombreAjour} a jour, {import.NombreNonAjour} non a jour, {import.NombreAVerifier} a verifier.";
            return RedirectToAction(nameof(Index), new { annee = anneeReference, importId = import.Id });
        }
    }

    private static CotisationNationaleImport? ResolveSelectedImport(Guid? importId, IReadOnlyCollection<CotisationNationaleImport> imports, int year)
    {
        if (importId.HasValue && importId.Value != Guid.Empty)
        {
            return imports.FirstOrDefault(i => i.Id == importId.Value);
        }

        return imports.FirstOrDefault(i => i.AnneeReference == year);
    }

    private InscriptionAnnuelleScout EnsureInscription(IDictionary<Guid, InscriptionAnnuelleScout> inscriptionsByScoutId, Scout scout, int anneeReference)
    {
        if (inscriptionsByScoutId.TryGetValue(scout.Id, out var inscription))
        {
            ApplySnapshot(inscription, scout);
            return inscription;
        }

        inscription = new InscriptionAnnuelleScout
        {
            Id = Guid.NewGuid(),
            ScoutId = scout.Id,
            Scout = scout,
            AnneeReference = anneeReference,
            LibelleAnnee = BuildYearLabel(anneeReference),
            DateInscription = DateTime.UtcNow,
            Statut = StatutInscriptionAnnuelle.Enregistree,
            InscriptionParoissialeValidee = false,
            CotisationNationaleAjour = false
        };
        ApplySnapshot(inscription, scout);

        inscriptionsByScoutId[scout.Id] = inscription;
        db.InscriptionsAnnuellesScouts.Add(inscription);
        return inscription;
    }

    private void UpsertCotisationTransaction(IDictionary<Guid, TransactionFinanciere> transactionsByScoutId, Scout scout, int anneeReference, decimal montant, Guid createurId)
    {
        if (transactionsByScoutId.TryGetValue(scout.Id, out var transaction))
        {
            transaction.Libelle = BuildTransactionLabel(anneeReference, scout);
            transaction.Montant = montant;
            transaction.Type = TypeTransaction.Recette;
            transaction.Categorie = CategorieFinance.Cotisation;
            transaction.DateTransaction = DateTime.UtcNow;
            transaction.Reference = BuildTransactionReference(anneeReference, scout.Matricule);
            transaction.Commentaire = "Transaction synchronisee depuis l'import des cotisations nationales.";
            transaction.GroupeId = scout.GroupeId;
            transaction.ScoutId = scout.Id;
            transaction.EstSupprime = false;
            return;
        }

        transaction = new TransactionFinanciere
        {
            Id = Guid.NewGuid(),
            Libelle = BuildTransactionLabel(anneeReference, scout),
            Montant = montant,
            Type = TypeTransaction.Recette,
            Categorie = CategorieFinance.Cotisation,
            DateTransaction = DateTime.UtcNow,
            Reference = BuildTransactionReference(anneeReference, scout.Matricule),
            Commentaire = "Transaction synchronisee depuis l'import des cotisations nationales.",
            GroupeId = scout.GroupeId,
            ScoutId = scout.Id,
            CreateurId = createurId
        };

        transactionsByScoutId[scout.Id] = transaction;
        db.TransactionsFinancieres.Add(transaction);
    }

    private static void SyncScoutCurrentCotisation(Scout scout, int anneeReference, bool value)
    {
        if (anneeReference == DateTime.UtcNow.Year)
        {
            scout.AssuranceAnnuelle = value;
        }
    }

    private static void ApplySnapshot(InscriptionAnnuelleScout inscription, Scout scout)
    {
        inscription.GroupeId ??= scout.GroupeId;
        inscription.BrancheId ??= scout.BrancheId;
        inscription.FonctionSnapshot ??= string.IsNullOrWhiteSpace(scout.Fonction) ? null : scout.Fonction.Trim();
    }

    private static bool HasIdentityMismatch(string? nomImporte, string? prenomImporte, Scout scout)
    {
        var nomMismatch = !string.IsNullOrWhiteSpace(nomImporte)
            && !string.Equals(NormalizeLookup(nomImporte), NormalizeLookup(scout.Nom), StringComparison.Ordinal);
        var prenomMismatch = !string.IsNullOrWhiteSpace(prenomImporte)
            && !string.Equals(NormalizeLookup(prenomImporte), NormalizeLookup(scout.Prenom), StringComparison.Ordinal);
        return nomMismatch || prenomMismatch;
    }

    private static bool TryReadDecimal(IXLRow row, IDictionary<string, int> headerMap, out decimal? value, out string errorMessage, params string[] headers)
    {
        value = null;
        errorMessage = string.Empty;

        if (!TryFindColumn(headerMap, out var column, headers))
        {
            return true;
        }

        var cell = row.Cell(column);
        if (cell.IsEmpty())
        {
            return true;
        }

        if (cell.DataType == XLDataType.Number)
        {
            value = Convert.ToDecimal(cell.GetDouble(), CultureInfo.InvariantCulture);
            return true;
        }

        var raw = cell.GetString().Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.GetCultureInfo("fr-FR"), out var parsed)
            || decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
        {
            value = parsed;
            return true;
        }

        errorMessage = "Montant invalide sur une ligne du fichier national.";
        return false;
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

    private static string NormalizeHeader(string value) => DatabaseText.NormalizeSearchKey(value);

    private static string NormalizeLookup(string? value) => DatabaseText.NormalizeSearchKey(value);

    private static string BuildYearLabel(int year) => $"{year}-{year + 1}";

    private static string BuildTransactionReferencePrefix(int anneeReference) => $"COTNAT-{anneeReference}-";

    private static string BuildTransactionReference(int anneeReference, string matricule) => $"{BuildTransactionReferencePrefix(anneeReference)}{ScoutMatriculeFormat.Normalize(matricule)}";

    private static string BuildTransactionLabel(int anneeReference, Scout scout) => $"Cotisation nationale {anneeReference} - {scout.Prenom} {scout.Nom}".Trim();

    private static string AppendObservation(string? currentValue, string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return currentValue ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(currentValue))
        {
            return note.Trim();
        }

        return currentValue.Contains(note, StringComparison.OrdinalIgnoreCase)
            ? currentValue
            : $"{currentValue} {note}".Trim();
    }

    private static byte[] GenerateTemplate()
    {
        using var workbook = new XLWorkbook();
        var data = workbook.Worksheets.Add("CotisationsNationales");
        var headers = new[] { "Matricule", "Nom", "Prenom", "Montant" };
        for (var i = 0; i < headers.Length; i++)
        {
            data.Cell(1, i + 1).Value = headers[i];
            data.Cell(1, i + 1).Style.Font.Bold = true;
            data.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#EAF4D7");
        }

        data.Cell(2, 1).Value = ScoutMatriculeFormat.Example;
        data.Cell(2, 2).Value = "DOE";
        data.Cell(2, 3).Value = "Jean";
        data.Cell(2, 4).Value = 1000;
        data.Columns().AdjustToContents();
        data.SheetView.FreezeRows(1);

        var guide = workbook.Worksheets.Add("Guide");
        guide.Cell("A1").Value = "Colonnes obligatoires";
        guide.Cell("A2").Value = "Matricule";
        guide.Cell("A4").Value = "Colonnes optionnelles";
        guide.Cell("A5").Value = "Nom, Prenom, Montant";
        guide.Cell("A7").Value = "Regles";
        guide.Cell("A8").Value = $"Le matricule doit respecter le format {ScoutMatriculeFormat.Example}.";
        guide.Cell("A9").Value = "Le rapprochement produit les statuts A jour, Non a jour et A verifier.";
        guide.Cell("A10").Value = "Les scouts absents du fichier national passent en Non a jour pour l'annee importee.";
        guide.Cell("A11").Value = "Les lignes douteuses restent dans A verifier pour traitement manuel.";
        guide.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}

