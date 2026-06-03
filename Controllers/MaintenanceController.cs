using System.Text;
using System.Text.Json;
using MangoTaika.Data;
using MangoTaika.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

[Authorize(Roles = "Administrateur")]
public class MaintenanceController(AppDbContext db) : Controller
{
    public IActionResult Index() => View();

    public async Task<IActionResult> Backup()
    {
        var snapshot = new
        {
            GeneratedAt = DateTime.UtcNow,
            Users = await db.Users.AsNoTracking().Select(u => new { u.Id, u.Nom, u.Prenom, u.Email, u.PhoneNumber, u.IsActive, u.EstSupprime, u.DateCreation }).ToListAsync(),
            Scouts = await db.Scouts.AsNoTracking().ToListAsync(),
            Parents = await db.Parents.AsNoTracking().ToListAsync(),
            Groupes = await db.Groupes.AsNoTracking().ToListAsync(),
            Branches = await db.Branches.AsNoTracking().ToListAsync(),
            Activites = await db.Activites.IgnoreQueryFilters().AsNoTracking().ToListAsync(),
            Demandes = await db.DemandesAutorisation.AsNoTracking().ToListAsync(),
            Transactions = await db.TransactionsFinancieres.IgnoreQueryFilters().AsNoTracking().ToListAsync(),
            Portefeuilles = await db.PortefeuillesUtilisateurs.AsNoTracking().ToListAsync(),
            MouvementsPortefeuilles = await db.MouvementsPortefeuilles.AsNoTracking().ToListAsync(),
            Dons = await db.DonsPublics.AsNoTracking().ToListAsync(),
            ArticlesBoutique = await db.ArticlesBoutique.IgnoreQueryFilters().AsNoTracking().ToListAsync(),
            CommandesBoutique = await db.CommandesBoutique.AsNoTracking().ToListAsync(),
            ProfilsAbonnements = await db.ProfilsAbonnements.IgnoreQueryFilters().AsNoTracking().ToListAsync(),
            AbonnementsUtilisateurs = await db.AbonnementsUtilisateurs.IgnoreQueryFilters().AsNoTracking().ToListAsync()
        };

        var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
        {
            WriteIndented = true,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
        });
        var bytes = Encoding.UTF8.GetBytes(json);
        return File(bytes, "application/json", $"mangotaika-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetOperationalData(string confirmation)
    {
        if (!string.Equals(confirmation?.Trim(), "RAZ MANGO TAIKA", StringComparison.Ordinal))
        {
            TempData["Error"] = "RAZ refusee : saisissez exactement RAZ MANGO TAIKA.";
            return RedirectToAction(nameof(Index));
        }

        await SoftDeleteAsync(await db.Activites.IgnoreQueryFilters().Where(a => !a.EstSupprime).ToListAsync());
        await SoftDeleteAsync(await db.TransactionsFinancieres.IgnoreQueryFilters().Where(t => !t.EstSupprime).ToListAsync());
        await SoftDeleteAsync(await db.ArticlesBoutique.IgnoreQueryFilters().Where(a => !a.EstSupprime).ToListAsync());
        await SoftDeleteAsync(await db.Galeries.IgnoreQueryFilters().Where(g => !g.EstSupprime).ToListAsync());
        await SoftDeleteAsync(await db.LivreDor.IgnoreQueryFilters().Where(l => !l.EstSupprime).ToListAsync());
        await SoftDeleteAsync(await db.ContactMessages.IgnoreQueryFilters().Where(m => !m.EstSupprime).ToListAsync());
        await SoftDeleteAsync(await db.Tickets.IgnoreQueryFilters().Where(t => !t.EstSupprime).ToListAsync());

        TempData["Success"] = "RAZ operationnelle appliquee. Les donnees sont masquees, pas effacees physiquement.";
        return RedirectToAction(nameof(Index));
    }

    private async Task SoftDeleteAsync<T>(IEnumerable<T> entities) where T : class
    {
        foreach (var entity in entities)
        {
            var property = entity.GetType().GetProperty("EstSupprime");
            if (property?.PropertyType == typeof(bool))
            {
                property.SetValue(entity, true);
            }
        }

        await db.SaveChangesAsync();
    }
}
