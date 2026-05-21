using MangoTaika.Data;
using MangoTaika.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

[Authorize(Roles = "Administrateur,Gestionnaire")]
public class MessagesController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? type)
    {
        var query = db.ContactMessages.Where(m => !m.EstSupprime).AsQueryable();
        if (!string.IsNullOrEmpty(type))
            query = query.Where(m => m.Type == type);

        var model = new MessagesAdminViewModel
        {
            Messages = await query.OrderByDescending(m => m.DateEnvoi).ToListAsync(),
            LivreDorMessages = await db.LivreDor
                .Where(m => !m.EstSupprime)
                .OrderBy(m => m.EstValide)
                .ThenByDescending(m => m.DateCreation)
                .ToListAsync(),
            TypeFiltre = type
        };

        return View(model);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var msg = await db.ContactMessages.FirstOrDefaultAsync(m => m.Id == id && !m.EstSupprime);
        if (msg is null) return NotFound();
        return View(msg);
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarquerLu(Guid id)
    {
        var msg = await db.ContactMessages.FindAsync(id);
        if (msg is not null)
        {
            msg.EstLu = true;
            await db.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Supprimer(Guid id)
    {
        var msg = await db.ContactMessages.FindAsync(id);
        if (msg is not null)
        {
            msg.EstSupprime = true;
            await db.SaveChangesAsync();
            TempData["Success"] = "Message supprimé.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ValiderLivreDor(Guid id)
    {
        var message = await db.LivreDor.FindAsync(id);
        if (message is not null && !message.EstSupprime)
        {
            message.EstValide = true;
            message.DateValidation = DateTime.UtcNow;
            await db.SaveChangesAsync();
            TempData["Success"] = "Message du livre d'or valide.";
        }

        return RedirectToAction(nameof(Index), new { type = "LivreDor" });
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SupprimerLivreDor(Guid id)
    {
        var message = await db.LivreDor.FindAsync(id);
        if (message is not null)
        {
            message.EstSupprime = true;
            await db.SaveChangesAsync();
            TempData["Success"] = "Message du livre d'or supprime.";
        }

        return RedirectToAction(nameof(Index), new { type = "LivreDor" });
    }
}
