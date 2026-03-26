using MangoTaika.Data;
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

        var messages = await query.OrderByDescending(m => m.DateEnvoi).ToListAsync();
        ViewBag.TypeFiltre = type;
        return View(messages);
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
}
