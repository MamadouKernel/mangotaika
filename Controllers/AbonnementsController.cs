using MangoTaika.Data;
using MangoTaika.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

[Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
public class AbonnementsController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.ComptesPaiement = await db.ComptesPaiementMobile
            .Where(c => c.EstActif && !c.EstSupprime && !c.EstPrincipal && c.ActiviteId == null)
            .OrderBy(c => c.Libelle)
            .ToListAsync();
        ViewBag.Utilisateurs = await db.Users
            .Where(u => u.IsActive && !u.EstSupprime)
            .OrderBy(u => u.Nom)
            .ThenBy(u => u.Prenom)
            .ToListAsync();

        var profils = await db.ProfilsAbonnements
            .Include(p => p.ComptePaiementMobile)
            .Include(p => p.Abonnements).ThenInclude(a => a.User)
            .OrderBy(p => p.NomProfil)
            .ToListAsync();

        return View(profils);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreerProfil(
        string nomProfil,
        PeriodiciteAbonnement periodicite,
        decimal montant,
        int delaiHoldJours,
        Guid comptePaiementMobileId)
    {
        if (string.IsNullOrWhiteSpace(nomProfil) || montant <= 0 || comptePaiementMobileId == Guid.Empty)
        {
            TempData["Error"] = "Profil incomplet : renseignez le nom, le montant et un compte de paiement global non principal.";
            return RedirectToAction(nameof(Index));
        }

        var compte = await db.ComptesPaiementMobile.FirstOrDefaultAsync(c =>
            c.Id == comptePaiementMobileId
            && c.EstActif
            && !c.EstSupprime
            && !c.EstPrincipal
            && c.ActiviteId == null);
        if (compte is null)
        {
            TempData["Error"] = "Le compte de paiement selectionne doit etre actif, global et non principal.";
            return RedirectToAction(nameof(Index));
        }

        db.ProfilsAbonnements.Add(new ProfilAbonnement
        {
            Id = Guid.NewGuid(),
            NomProfil = nomProfil.Trim(),
            Periodicite = periodicite,
            Montant = montant,
            DelaiHoldJours = Math.Max(0, delaiHoldJours),
            ComptePaiementMobileId = compte.Id
        });
        await db.SaveChangesAsync();
        TempData["Success"] = "Profil abonne cree.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Affecter(Guid userId, Guid profilAbonnementId)
    {
        var userExists = await db.Users.AnyAsync(u => u.Id == userId && u.IsActive && !u.EstSupprime);
        var profil = await db.ProfilsAbonnements.FirstOrDefaultAsync(p => p.Id == profilAbonnementId && p.IsActive);
        if (!userExists || profil is null)
        {
            TempData["Error"] = "Affectation impossible : utilisateur ou profil introuvable.";
            return RedirectToAction(nameof(Index));
        }

        var existing = await db.AbonnementsUtilisateurs.FirstOrDefaultAsync(a => a.UserId == userId && a.ProfilAbonnementId == profilAbonnementId);
        if (existing is not null)
        {
            existing.Statut = StatutAbonnement.Actif;
            existing.DateEcheance = ComputeDueDate(DateTime.UtcNow, profil.Periodicite);
        }
        else
        {
            db.AbonnementsUtilisateurs.Add(new AbonnementUtilisateur
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ProfilAbonnementId = profilAbonnementId,
                DateDebut = DateTime.UtcNow,
                DateEcheance = ComputeDueDate(DateTime.UtcNow, profil.Periodicite)
            });
        }

        await db.SaveChangesAsync();
        TempData["Success"] = "Profil abonne rattache au compte utilisateur.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DesactiverProfil(Guid id)
    {
        var profil = await db.ProfilsAbonnements.FirstOrDefaultAsync(p => p.Id == id);
        if (profil is null) return NotFound();

        profil.IsActive = false;
        await db.SaveChangesAsync();
        TempData["Success"] = "Profil abonne desactive.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SupprimerProfil(Guid id)
    {
        var profil = await db.ProfilsAbonnements.FirstOrDefaultAsync(p => p.Id == id);
        if (profil is null) return NotFound();

        profil.EstSupprime = true;
        profil.IsActive = false;
        await db.SaveChangesAsync();
        TempData["Success"] = "Profil abonne supprime de la liste.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AppliquerHold()
    {
        var now = DateTime.UtcNow;
        var abonnes = await db.AbonnementsUtilisateurs
            .Include(a => a.User)
            .Include(a => a.ProfilAbonnement)
            .Where(a => a.ProfilAbonnement.IsActive && a.Statut != StatutAbonnement.Inactif)
            .ToListAsync();

        var holdCount = 0;
        var inactivatedCount = 0;
        foreach (var abonne in abonnes)
        {
            if (abonne.DateEcheance >= now)
            {
                continue;
            }

            var inactivationDate = abonne.DateEcheance.AddDays(Math.Max(0, abonne.ProfilAbonnement.DelaiHoldJours));
            if (inactivationDate <= now)
            {
                if (abonne.Statut != StatutAbonnement.Inactif)
                {
                    abonne.Statut = StatutAbonnement.Inactif;
                    inactivatedCount++;
                }

                abonne.User.IsActive = false;
            }
            else if (abonne.Statut != StatutAbonnement.EnHold)
            {
                abonne.Statut = StatutAbonnement.EnHold;
                holdCount++;
            }
        }

        await db.SaveChangesAsync();
        TempData["Success"] = $"Controle abonnements applique : {holdCount} compte(s) en hold, {inactivatedCount} compte(s) inactive(s).";
        return RedirectToAction(nameof(Index));
    }

    private static DateTime ComputeDueDate(DateTime start, PeriodiciteAbonnement periodicite)
        => periodicite switch
        {
            PeriodiciteAbonnement.Trimestrielle => start.AddMonths(3),
            PeriodiciteAbonnement.Semestrielle => start.AddMonths(6),
            PeriodiciteAbonnement.Annuelle => start.AddYears(1),
            _ => start.AddMonths(1)
        };
}
