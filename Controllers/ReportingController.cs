using ClosedXML.Excel;
using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

[Authorize(Roles = "Administrateur,Gestionnaire,Superviseur,Consultant")]
public class ReportingController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var year = DateTime.Now.Year;
        var jsonOpts = new System.Text.Json.JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        // Scouts par branche (cumul sur les 6 branches canoniques)
        var scoutsParBrancheRaw = await db.Branches
            .Where(b => b.IsActive)
            .Select(b => new
            {
                b.Nom,
                Count = db.Scouts.Count(s => s.BrancheId == b.Id && s.IsActive)
            })
            .ToListAsync();

        var ordreBranches = new[]
        {
            BrancheReportingLabels.Oisillon,
            BrancheReportingLabels.Louveteau,
            BrancheReportingLabels.Eclaireur,
            BrancheReportingLabels.Cheminot,
            BrancheReportingLabels.Route,
            BrancheReportingLabels.Ads
        };

        var scoutsParBranche = scoutsParBrancheRaw
            .GroupBy(x => NormalizeReportingBranchLabel(x.Nom))
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Count));

        var scoutsParBrancheOrdered = ordreBranches
            .Select(label => new
            {
                Nom = label,
                Count = scoutsParBranche.TryGetValue(label, out var total) ? total : 0
            })
            .ToList();
        ViewBag.ScoutsParBrancheJson = System.Text.Json.JsonSerializer.Serialize(scoutsParBrancheOrdered, jsonOpts);

        // Scouts par groupe
        var scoutsParGroupe = await db.Groupes
            .Where(g => g.IsActive)
            .Select(g => new { Nom = g.Nom, Count = db.Scouts.Count(s => s.GroupeId == g.Id && s.IsActive) })
            .Where(x => x.Count > 0)
            .ToListAsync();
        ViewBag.ScoutsParGroupeJson = System.Text.Json.JsonSerializer.Serialize(scoutsParGroupe, jsonOpts);

        // Finances mensuelles
        var financesMensuelles = await db.TransactionsFinancieres
            .Where(t => !t.EstSupprime && t.DateTransaction.Year == year)
            .GroupBy(t => t.DateTransaction.Month)
            .Select(g => new
            {
                Mois = g.Key,
                Recettes = g.Where(t => t.Type == TypeTransaction.Recette).Sum(t => t.Montant),
                Depenses = g.Where(t => t.Type == TypeTransaction.Depense).Sum(t => t.Montant)
            })
            .OrderBy(x => x.Mois)
            .ToListAsync();
        ViewBag.FinancesMensuellesJson = System.Text.Json.JsonSerializer.Serialize(financesMensuelles, jsonOpts);

        // ActivitÃ©s par type
        var activitesParType = await db.Activites
            .Where(a => !a.EstSupprime)
            .GroupBy(a => a.Type)
            .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();
        ViewBag.ActivitesParTypeJson = System.Text.Json.JsonSerializer.Serialize(activitesParType, jsonOpts);

        // Tickets par statut
        var ticketsParStatut = await db.Tickets
            .Where(t => !t.EstSupprime)
            .GroupBy(t => t.Statut)
            .Select(g => new { Statut = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();
        ViewBag.TicketsParStatutJson = System.Text.Json.JsonSerializer.Serialize(ticketsParStatut, jsonOpts);

        // Ã‰volution inscriptions par mois
        var inscriptionsParMois = await db.Scouts
            .Where(s => s.IsActive && s.DateInscription.Year == year)
            .GroupBy(s => s.DateInscription.Month)
            .Select(g => new { Mois = g.Key, Count = g.Count() })
            .OrderBy(x => x.Mois)
            .ToListAsync();
        ViewBag.InscriptionsParMoisJson = System.Text.Json.JsonSerializer.Serialize(inscriptionsParMois, jsonOpts);

        ViewBag.Annee = year;
        return View();
    }
    private static string NormalizeReportingBranchLabel(string? brancheNom)
    {
        var normalized = DatabaseText.NormalizeSearchKey(brancheNom ?? string.Empty);

        return normalized switch
        {
            var value when value.Contains("OISILLON") || value.Contains("COLONIE")
                => BrancheReportingLabels.Oisillon,
            var value when value.Contains("LOUVETEAU") || value.Contains("LOUVETEAUX") || value.Contains("MEUTE")
                => BrancheReportingLabels.Louveteau,
            var value when value.Contains("ECLAIREUR") || value.Contains("ECLAIREURS") || value.Contains("TROUPE")
                => BrancheReportingLabels.Eclaireur,
            var value when value.Contains("CHEMINOT") || value.Contains("GENERATION")
                => BrancheReportingLabels.Cheminot,
            var value when value.Contains("ROUTE") || value.Contains("ROUTIER") || value.Contains("ROUTIERS") || value.Contains("COMMUNAUTE")
                => BrancheReportingLabels.Route,
            var value when value.Contains("ADS") || value.Contains("ADULTE") || value.Contains("BENEVOLE")
                => BrancheReportingLabels.Ads,
            _ => BrancheReportingLabels.Ads
        };
    }

    private static class BrancheReportingLabels
    {
        public const string Oisillon = "BRANCHE OISILLON (4 - 7 ANS) - LA COLONIE";
        public const string Louveteau = "BRANCHE LOUVETEAU (8 - 11 ANS) - LA MEUTE";
        public const string Eclaireur = "BRANCHE ECLAIREUR (12 - 14 ANS) - LA TROUPE";
        public const string Cheminot = "BRANCHE CHEMINOT (15 - 17 ANS) - LA GENERATION";
        public const string Route = "BRANCHE ROUTE (18 - 21 ANS) - LA COMMUNAUTE";
        public const string Ads = "LES BENEVOLES - Adultes Dans le Scoutisme (ADS)";
    }
    public async Task<IActionResult> ExportScouts()
    {
        var scouts = await db.Scouts
            .Include(s => s.Groupe).Include(s => s.Branche)
            .Where(s => s.IsActive)
            .OrderBy(s => s.Nom).ThenBy(s => s.Prenom)
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Scouts");
        ws.Cell(1, 1).Value = "Matricule";
        ws.Cell(1, 2).Value = "NÂ° Carte";
        ws.Cell(1, 3).Value = "Nom";
        ws.Cell(1, 4).Value = "PrÃ©nom";
        ws.Cell(1, 5).Value = "Date Naissance";
        ws.Cell(1, 6).Value = "Sexe";
        ws.Cell(1, 7).Value = "RÃ©gion Scoute";
        ws.Cell(1, 8).Value = "District";
        ws.Cell(1, 9).Value = "Groupe";
        ws.Cell(1, 10).Value = "Branche";
        ws.Cell(1, 11).Value = "Fonction";
        ws.Cell(1, 12).Value = "TÃ©lÃ©phone";
        ws.Cell(1, 13).Value = "Email";
        ws.Row(1).Style.Font.Bold = true;
        ws.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#597537");
        ws.Row(1).Style.Font.FontColor = XLColor.White;

        for (int i = 0; i < scouts.Count; i++)
        {
            var s = scouts[i];
            var r = i + 2;
            ws.Cell(r, 1).Value = s.Matricule;
            ws.Cell(r, 2).Value = s.NumeroCarte ?? "";
            ws.Cell(r, 3).Value = s.Nom;
            ws.Cell(r, 4).Value = s.Prenom;
            ws.Cell(r, 5).Value = s.DateNaissance.ToString("dd/MM/yyyy");
            ws.Cell(r, 6).Value = s.Sexe ?? "";
            ws.Cell(r, 7).Value = s.RegionScoute ?? "";
            ws.Cell(r, 8).Value = s.District ?? "";
            ws.Cell(r, 9).Value = s.Groupe?.Nom ?? "";
            ws.Cell(r, 10).Value = s.Branche?.Nom ?? "";
            ws.Cell(r, 11).Value = s.Fonction ?? "";
            ws.Cell(r, 12).Value = s.Telephone ?? "";
            ws.Cell(r, 13).Value = s.Email ?? "";
        }
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Scouts_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    public async Task<IActionResult> ExportFinances(int? annee)
    {
        var year = annee ?? DateTime.Now.Year;
        var transactions = await db.TransactionsFinancieres
            .Include(t => t.Groupe).Include(t => t.Scout)
            .Where(t => !t.EstSupprime && t.DateTransaction.Year == year)
            .OrderByDescending(t => t.DateTransaction)
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add($"Finances {year}");
        ws.Cell(1, 1).Value = "Date";
        ws.Cell(1, 2).Value = "LibellÃ©";
        ws.Cell(1, 3).Value = "Type";
        ws.Cell(1, 4).Value = "CatÃ©gorie";
        ws.Cell(1, 5).Value = "Montant (FCFA)";
        ws.Cell(1, 6).Value = "Groupe";
        ws.Cell(1, 7).Value = "Scout";
        ws.Cell(1, 8).Value = "RÃ©fÃ©rence";
        ws.Row(1).Style.Font.Bold = true;
        ws.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#597537");
        ws.Row(1).Style.Font.FontColor = XLColor.White;

        for (int i = 0; i < transactions.Count; i++)
        {
            var t = transactions[i];
            var r = i + 2;
            ws.Cell(r, 1).Value = t.DateTransaction.ToString("dd/MM/yyyy");
            ws.Cell(r, 2).Value = t.Libelle;
            ws.Cell(r, 3).Value = t.Type.ToString();
            ws.Cell(r, 4).Value = t.Categorie.ToString();
            ws.Cell(r, 5).Value = t.Montant;
            ws.Cell(r, 6).Value = t.Groupe?.Nom ?? "";
            ws.Cell(r, 7).Value = t.Scout != null ? $"{t.Scout.Nom} {t.Scout.Prenom}" : "";
            ws.Cell(r, 8).Value = t.Reference ?? "";
        }

        // Ligne totaux
        var lastRow = transactions.Count + 2;
        ws.Cell(lastRow, 4).Value = "TOTAL RECETTES";
        ws.Cell(lastRow, 4).Style.Font.Bold = true;
        ws.Cell(lastRow, 5).Value = transactions.Where(t => t.Type == TypeTransaction.Recette).Sum(t => t.Montant);
        ws.Cell(lastRow, 5).Style.Font.FontColor = XLColor.Green;
        ws.Cell(lastRow, 5).Style.Font.Bold = true;
        ws.Cell(lastRow + 1, 4).Value = "TOTAL DÃ‰PENSES";
        ws.Cell(lastRow + 1, 4).Style.Font.Bold = true;
        ws.Cell(lastRow + 1, 5).Value = transactions.Where(t => t.Type == TypeTransaction.Depense).Sum(t => t.Montant);
        ws.Cell(lastRow + 1, 5).Style.Font.FontColor = XLColor.Red;
        ws.Cell(lastRow + 1, 5).Style.Font.Bold = true;
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Finances_{year}_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    public async Task<IActionResult> ExportActivites()
    {
        var activites = await db.Activites
            .Include(a => a.Groupe).Include(a => a.Participants)
            .Where(a => !a.EstSupprime)
            .OrderByDescending(a => a.DateDebut)
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("ActivitÃ©s");
        ws.Cell(1, 1).Value = "Titre";
        ws.Cell(1, 2).Value = "Type";
        ws.Cell(1, 3).Value = "Date DÃ©but";
        ws.Cell(1, 4).Value = "Date Fin";
        ws.Cell(1, 5).Value = "Lieu";
        ws.Cell(1, 6).Value = "Groupe";
        ws.Cell(1, 7).Value = "Statut";
        ws.Cell(1, 8).Value = "Participants";
        ws.Cell(1, 9).Value = "Budget (FCFA)";
        ws.Row(1).Style.Font.Bold = true;
        ws.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#597537");
        ws.Row(1).Style.Font.FontColor = XLColor.White;

        for (int i = 0; i < activites.Count; i++)
        {
            var a = activites[i];
            var r = i + 2;
            ws.Cell(r, 1).Value = a.Titre;
            ws.Cell(r, 2).Value = a.Type.ToString();
            ws.Cell(r, 3).Value = a.DateDebut.ToString("dd/MM/yyyy");
            ws.Cell(r, 4).Value = a.DateFin?.ToString("dd/MM/yyyy") ?? "";
            ws.Cell(r, 5).Value = a.Lieu ?? "";
            ws.Cell(r, 6).Value = a.Groupe?.Nom ?? "";
            ws.Cell(r, 7).Value = a.Statut.ToString();
            ws.Cell(r, 8).Value = a.Participants.Count;
            ws.Cell(r, 9).Value = a.BudgetPrevisionnel ?? 0;
        }
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Activites_{DateTime.Now:yyyyMMdd}.xlsx");
    }
}


