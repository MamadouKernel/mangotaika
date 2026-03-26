using MangoTaika.Data;
using MangoTaika.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MangoTaika.Data.Entities;

namespace MangoTaika.Controllers;

[Authorize]
public class NotificationsController(AppDbContext db, UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Mine(int take = 20)
    {
        var userId = Guid.Parse(userManager.GetUserId(User)!);
        var limit = Math.Clamp(take, 1, 50);
        var items = await db.NotificationsUtilisateur
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.DateCreation)
            .Take(limit)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Titre = n.Titre,
                Message = n.Message,
                Categorie = n.Categorie,
                Lien = n.Lien,
                EstLue = n.EstLue,
                DateCreation = n.DateCreation
            })
            .ToListAsync();

        return Json(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = Guid.Parse(userManager.GetUserId(User)!);
        var now = DateTime.UtcNow;
        var notifications = await db.NotificationsUtilisateur
            .Where(n => n.UserId == userId && !n.EstLue)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.EstLue = true;
            notification.DateLecture = now;
        }

        await db.SaveChangesAsync();
        return Ok(new { success = true, count = notifications.Count });
    }
}
