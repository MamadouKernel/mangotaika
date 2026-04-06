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

            var requiredHeaders = new[] { "montant" };
            var missingHeaders = requiredHeaders.Where(h => !headerMap.ContainsKey(h)).ToList();
            if (missingHeaders.Count != 0)
            {
                TempData["Error"] = $"Colonnes obligatoires manquantes dans le fichier national : {string.Join(", ", missingHeaders)}.";
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
            var scoutsByIdentity = activeScouts
                .GroupBy(s => BuildScoutIdentityKey(s.Nom, s.Prenom, s.DateNaissance))
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
            var reservedMatricules = new HashSet<string>(scoutsByMatricule.Keys, StringComparer.OrdinalIgnoreCase);

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
            var processedScoutIds = new HashSet<Guid>();
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

                var matriculeImporte = ScoutMatriculeFormat.NormalizeOptional(ReadString(row, headerMap, "matricule"));
                var nomImporte = ReadString(row, headerMap, "nom", "nomscout");
                var prenomImporte = ReadString(row, headerMap, "prenom", "prenomscout");
                var nomComplet = string.Join(" ", new[] { prenomImporte, nomImporte }.Where(v => !string.IsNullOrWhiteSpace(v))).Trim();
                var dateNaissanceImportee = ReadOptionalDate(row, headerMap, "datenaissance", "date_naissance", "dateofbirth");
                var identityKey = BuildScoutIdentityKeyOrDefault(nomImporte, prenomImporte, dateNaissanceImportee);
                var lookupKey = NormalizeLookup(matriculeImporte);
                var raisons = new List<string>();

                if (!TryReadDecimal(row, headerMap, out var montant, out var montantErreur, "montant", "montantfcfa", "montantcotisation"))
                {
                    raisons.Add(montantErreur);
                }
                else if (!montant.HasValue)
                {
                    raisons.Add("Le montant est obligatoire pour chaque ligne du fichier national.");
                }

                if (!string.IsNullOrWhiteSpace(matriculeImporte))
                {
                    if (!ScoutMatriculeFormat.IsValid(matriculeImporte))
                    {
                        raisons.Add($"Matricule invalide: {matriculeImporte}. Format attendu: {ScoutMatriculeFormat.Example}.");
                    }
                    else if (!seenMatricules.Add(lookupKey))
                    {
                        raisons.Add("Matricule duplique dans le fichier national.");
                    }
                }

                Scout? scout = null;
                var matchedByMatricule = false;
                if (!string.IsNullOrWhiteSpace(lookupKey) && scoutsByMatricule.TryGetValue(lookupKey, out var matchedScout))
                {
                    scout = matchedScout;
                    matchedByMatricule = true;
                }
                else if (!string.IsNullOrWhiteSpace(identityKey) && scoutsByIdentity.TryGetValue(identityKey, out var scoutsMemeIdentite))
                {
                    if (scoutsMemeIdentite.Count == 1)
                    {
                        scout = scoutsMemeIdentite[0];
                    }
                    else if (scoutsMemeIdentite.Count > 1)
                    {
                        raisons.Add("Plusieurs scouts actifs correspondent a l'identite fournie. Le rapprochement doit etre verifie manuellement.");
                    }
                }

                if (scout is null)
                {
                    if (string.IsNullOrWhiteSpace(matriculeImporte))
                    {
                        raisons.Add(string.IsNullOrWhiteSpace(identityKey)
                            ? "Nom, prenom et date de naissance sont requis pour rapprocher un scout sans matricule."
                            : "Aucun scout actif ne correspond a l'identite fournie.");
                    }
                    else
                    {
                        raisons.Add(string.IsNullOrWhiteSpace(identityKey)
                            ? "Aucun scout actif ne correspond a ce matricule."
                            : "Le matricule importe ne correspond a aucun scout actif. La ligne reste a verifier.");
                    }
                }
                else
                {
                    recognizedScoutIds.Add(scout.Id);
                    if (!processedScoutIds.Add(scout.Id))
                    {
                        raisons.Add("Le meme scout apparait plusieurs fois dans le fichier national.");
                    }

                    if (HasIdentityMismatch(nomImporte, prenomImporte, scout))
                    {
                        raisons.Add("Le nom ou le prenom importe ne correspond pas a la fiche scout.");
                    }
                }

                if (montant.HasValue && montant.Value < 0)
                {
                    raisons.Add("Le montant ne peut pas etre negatif.");
                }

                string? matriculeRapproche = scout?.Matricule;
                if (raisons.Count == 0 && scout is not null)
                {
                    if (!string.IsNullOrWhiteSpace(matriculeImporte))
                    {
                        if (matchedByMatricule)
                        {
                            matriculeRapproche = scout.Matricule;
                        }
                        else if (!string.IsNullOrWhiteSpace(scout.Matricule)
                            && !string.Equals(NormalizeLookup(scout.Matricule), lookupKey, StringComparison.OrdinalIgnoreCase))
                        {
                            raisons.Add("Le matricule importe ne correspond pas au matricule deja attribue a ce scout.");
                        }
                        else if (reservedMatricules.Contains(lookupKey) && (!scoutsByMatricule.TryGetValue(lookupKey, out var owner) || owner.Id != scout.Id))
                        {
                            raisons.Add("Le matricule importe est deja utilise par un autre scout.");
                        }
                        else
                        {
                            matriculeRapproche = matriculeImporte;
                        }
                    }
                    else if (string.IsNullOrWhiteSpace(scout.Matricule))
                    {
                        matriculeRapproche = GenerateNextMatricule(reservedMatricules, scout);
                    }
                }

                var ligne = new CotisationNationaleImportLigne
                {
                    Id = Guid.NewGuid(),
                    ImportId = import.Id,
                    Import = import,
                    ScoutId = scout?.Id,
                    Matricule = raisons.Count == 0 ? (matriculeRapproche ?? string.Empty) : (matriculeImporte ?? string.Empty),
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
                    if (!string.IsNullOrWhiteSpace(matriculeRapproche))
                    {
                        scout.Matricule = matriculeRapproche;
                        var finalMatriculeKey = NormalizeLookup(matriculeRapproche);
                        reservedMatricules.Add(finalMatriculeKey);
                        scoutsByMatricule[finalMatriculeKey] = scout;
                        ligne.Matricule = matriculeRapproche;
                    }

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
                    Matricule = scout.Matricule ?? string.Empty,
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

    private static string GenerateNextMatricule(ISet<string> reservedMatricules, Scout scout)
    {
        var maxSequence = 0;
        foreach (var value in reservedMatricules)
        {
            if (ScoutMatriculeFormat.TryParseParts(value, out var sequence, out _))
            {
                maxSequence = Math.Max(maxSequence, sequence);
            }
        }

        var nextSequence = maxSequence + 1;
        var suffixes = BuildPreferredMatriculeSuffixes(scout);
        while (true)
        {
            foreach (var suffix in suffixes)
            {
                var candidate = ScoutMatriculeFormat.Compose(nextSequence, suffix);
                if (reservedMatricules.Add(NormalizeLookup(candidate)))
                {
                    return candidate;
                }
            }

            nextSequence++;
        }
    }

    private static IReadOnlyList<char> BuildPreferredMatriculeSuffixes(Scout scout)
    {
        var suffixes = new List<char>();
        foreach (var character in $"{scout.Nom}{scout.Prenom}".ToUpperInvariant())
        {
            if (char.IsLetter(character) && !suffixes.Contains(character))
            {
                suffixes.Add(character);
            }
        }

        for (var character = 'A'; character <= 'Z'; character++)
        {
            if (!suffixes.Contains(character))
            {
                suffixes.Add(character);
            }
        }

        return suffixes;
    }

    private static string BuildYearLabel(int year) => $"{year}-{year + 1}";

    private static string BuildTransactionReferencePrefix(int anneeReference) => $"COTNAT-{anneeReference}-";

    private static string BuildTransactionReference(int anneeReference, string? matricule) => $"{BuildTransactionReferencePrefix(anneeReference)}{ScoutMatriculeFormat.Normalize(matricule)}";

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
        var headers = new[] { "Matricule", "Nom", "Prenom", "DateNaissance", "Montant" };
        for (var i = 0; i < headers.Length; i++)
        {
            data.Cell(1, i + 1).Value = headers[i];
            data.Cell(1, i + 1).Style.Font.Bold = true;
            data.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#EAF4D7");
        }

        data.Cell(2, 1).Value = string.Empty;
        data.Cell(2, 2).Value = "DOE";
        data.Cell(2, 3).Value = "Jean";
        data.Cell(2, 4).Value = new DateTime(2012, 5, 14);
        data.Cell(2, 5).Value = 1000;
        data.Cell(2, 4).Style.DateFormat.Format = "dd/MM/yyyy";
        data.Columns().AdjustToContents();
        data.SheetView.FreezeRows(1);

        var guide = workbook.Worksheets.Add("Guide");
        guide.Cell("A1").Value = "Colonnes obligatoires";
        guide.Cell("A2").Value = "Montant";
        guide.Cell("A4").Value = "Colonnes recommandees pour un premier rattachement";
        guide.Cell("A5").Value = "Nom, Prenom, DateNaissance";
        guide.Cell("A7").Value = "Regles";
        guide.Cell("A8").Value = $"Le matricule est optionnel. S'il est renseigne, il doit respecter le format {ScoutMatriculeFormat.Example}.";
        guide.Cell("A9").Value = "Sans matricule, le rapprochement d'un scout exige Nom, Prenom et DateNaissance.";
        guide.Cell("A10").Value = "A la premiere cotisation nationale, le matricule est attribue ou confirme automatiquement.";
        guide.Cell("A11").Value = "Le rapprochement produit les statuts A jour, Non a jour et A verifier.";
        guide.Cell("A12").Value = "Les scouts absents du fichier national passent en Non a jour pour l'annee importee.";
        guide.Cell("A13").Value = "Les lignes douteuses restent dans A verifier pour traitement manuel.";
        guide.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}



