using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
using MangoTaika.Helpers;
using MangoTaika.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

public class DemandesGroupeController(AppDbContext db, UserManager<ApplicationUser> userManager, IGeocodingService geocoding) : Controller
{
    // Page publique de demande
    [AllowAnonymous]
    public IActionResult Creer() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> Creer(DemandeGroupeCreateDto dto, string? Website)
    {
        // Honeypot anti-spam
        if (!string.IsNullOrEmpty(Website)) return RedirectToAction(nameof(Creer));

        if (!ModelState.IsValid) return View(dto);

        var demande = new DemandeGroupe
        {
            Id = Guid.NewGuid(),
            NomGroupe = dto.NomGroupe,
            Commune = dto.Commune,
            Quartier = dto.Quartier,
            NomResponsable = dto.NomResponsable,
            TelephoneResponsable = dto.TelephoneResponsable,
            EmailResponsable = dto.EmailResponsable,
            Motivation = dto.Motivation,
            NombreMembresPrevus = dto.NombreMembresPrevus
        };
        db.DemandesGroupe.Add(demande);
        await db.SaveChangesAsync();

        TempData["Success"] = "Votre demande de création de groupe a été enregistrée. Elle sera examinée par l'administration.";
        return RedirectToAction(nameof(Creer));
    }

    // Admin : liste des demandes
    [Authorize(Roles = "Administrateur,Gestionnaire,Superviseur,Consultant")]
    public async Task<IActionResult> Index()
    {
        var (page, ps) = ListPagination.Read(Request);
        var query = db.DemandesGroupe.OrderByDescending(d => d.DateCreation);
        var total = await query.CountAsync();
        var (p, pageSize, skip, totalPages) = ListPagination.Normalize(page, ps, total);
        var demandes = await query.Skip(skip).Take(pageSize).ToListAsync();
        ListPagination.SetViewData(ViewData, HttpContext, p, pageSize, total, totalPages);
        return View(demandes);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire,Superviseur,Consultant")]
    public async Task<IActionResult> Details(Guid id)
    {
        var demande = await db.DemandesGroupe
            .Include(d => d.TraitePar)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (demande is null) return NotFound();
        return View(demande);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Approuver(Guid id)
    {
        var demande = await db.DemandesGroupe.FindAsync(id);
        if (demande is null) return NotFound();

        var user = await userManager.GetUserAsync(User);
        demande.Statut = StatutDemandeGroupe.Approuvee;
        demande.DateTraitement = DateTime.UtcNow;
        demande.TraiteParId = user?.Id;

        // Créer le groupe automatiquement
        var adresse = $"{demande.Quartier}, {demande.Commune}";
        var (lat, lng) = await geocoding.GeocodeAsync(adresse);

        db.Groupes.Add(new Groupe
        {
            Id = Guid.NewGuid(),
            Nom = demande.NomGroupe,
            Description = $"Groupe créé suite à la demande de {demande.NomResponsable}",
            Adresse = adresse,
            Latitude = lat,
            Longitude = lng
        });

        await db.SaveChangesAsync();
        TempData["Success"] = "Demande approuvée et groupe créé.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Rejeter(Guid id, string? motif)
    {
        var demande = await db.DemandesGroupe.FindAsync(id);
        if (demande is null) return NotFound();

        var user = await userManager.GetUserAsync(User);
        demande.Statut = StatutDemandeGroupe.Rejetee;
        demande.MotifRejet = motif;
        demande.DateTraitement = DateTime.UtcNow;
        demande.TraiteParId = user?.Id;
        await db.SaveChangesAsync();

        TempData["Success"] = "Demande rejetée.";
        return RedirectToAction(nameof(Index));
    }
}
