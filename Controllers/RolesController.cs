using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
using MangoTaika.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

[Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
public class RolesController(
    AppDbContext db,
    RoleManager<IdentityRole<Guid>> roleManager) : Controller
{
    public async Task<IActionResult> Index(bool inclureSupprimes = false)
    {
        var roles = await roleManager.Roles.AsNoTracking().ToListAsync();
        var metadonnees = await db.RolesMetadonnees.AsNoTracking().ToListAsync();
        var systemeNames = RoleNames.Definitions.Select(d => d.Name).ToHashSet(StringComparer.Ordinal);

        var lignes = new List<RoleViewModel>();
        foreach (var role in roles)
        {
            if (role.Name is null)
            {
                continue;
            }

            var definition = RoleNames.Definitions.FirstOrDefault(d => d.Name == role.Name);
            var meta = metadonnees.FirstOrDefault(m => m.RoleId == role.Id);
            var estSysteme = systemeNames.Contains(role.Name);
            var estSupprime = meta?.EstSupprime ?? false;
            var estActif = meta?.EstActif ?? true;

            if (estSupprime && !inclureSupprimes)
            {
                continue;
            }

            lignes.Add(new RoleViewModel
            {
                Id = role.Id,
                Name = role.Name,
                Libelle = meta?.Libelle ?? definition?.Label ?? role.Name,
                Description = meta?.Description ?? definition?.Description,
                Visibilite = meta?.Visibilite ?? definition?.Visibility,
                Hierarchie = meta?.Hierarchie ?? definition?.Hierarchy ?? 50,
                EstSysteme = estSysteme,
                EstActif = estActif,
                EstSupprime = estSupprime,
                DateSuppression = meta?.DateSuppression
            });
        }

        ViewBag.InclureSupprimes = inclureSupprimes;
        return View(lignes.OrderBy(l => l.Hierarchie).ThenBy(l => l.Libelle).ToList());
    }

    public IActionResult Create()
    {
        return View(new RoleEditDto { Hierarchie = 50 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoleEditDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var nomTechnique = NormaliserNomTechnique(dto.Name);
        if (string.IsNullOrWhiteSpace(nomTechnique))
        {
            ModelState.AddModelError(nameof(dto.Name), "Le nom technique du role est obligatoire.");
            return View(dto);
        }

        if (await roleManager.RoleExistsAsync(nomTechnique))
        {
            ModelState.AddModelError(nameof(dto.Name), "Un role avec ce nom technique existe deja.");
            return View(dto);
        }

        var role = new IdentityRole<Guid>
        {
            Id = Guid.NewGuid(),
            Name = nomTechnique,
            NormalizedName = nomTechnique.ToUpperInvariant()
        };

        var result = await roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(dto);
        }

        db.RolesMetadonnees.Add(new RoleMetadonnee
        {
            Id = Guid.NewGuid(),
            RoleId = role.Id,
            Libelle = dto.Libelle?.Trim() ?? nomTechnique,
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            Visibilite = string.IsNullOrWhiteSpace(dto.Visibilite) ? null : dto.Visibilite.Trim(),
            Hierarchie = dto.Hierarchie,
            EstSysteme = false
        });
        await db.SaveChangesAsync();

        TempData["Success"] = $"Role \"{dto.Libelle}\" cree. Configurez ses permissions depuis la page Permissions.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var role = await roleManager.FindByIdAsync(id.ToString());
        if (role is null || role.Name is null)
        {
            return NotFound();
        }

        var meta = await db.RolesMetadonnees.AsNoTracking().FirstOrDefaultAsync(m => m.RoleId == id);
        var definition = RoleNames.Definitions.FirstOrDefault(d => d.Name == role.Name);
        var estSysteme = RoleNames.Definitions.Any(d => d.Name == role.Name);

        ViewBag.EstSysteme = estSysteme;
        ViewBag.RoleId = role.Id;
        ViewBag.RoleName = role.Name;

        return View(new RoleEditDto
        {
            Name = role.Name,
            Libelle = meta?.Libelle ?? definition?.Label ?? role.Name,
            Description = meta?.Description ?? definition?.Description,
            Visibilite = meta?.Visibilite ?? definition?.Visibility,
            Hierarchie = meta?.Hierarchie ?? definition?.Hierarchy ?? 50
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, RoleEditDto dto)
    {
        var role = await roleManager.FindByIdAsync(id.ToString());
        if (role is null || role.Name is null)
        {
            return NotFound();
        }

        var estSysteme = RoleNames.Definitions.Any(d => d.Name == role.Name);

        if (!ModelState.IsValid)
        {
            ViewBag.EstSysteme = estSysteme;
            ViewBag.RoleId = role.Id;
            ViewBag.RoleName = role.Name;
            return View(dto);
        }

        if (!estSysteme)
        {
            var nomTechnique = NormaliserNomTechnique(dto.Name);
            if (!string.Equals(nomTechnique, role.Name, StringComparison.Ordinal)
                && await roleManager.RoleExistsAsync(nomTechnique))
            {
                ModelState.AddModelError(nameof(dto.Name), "Un role avec ce nom technique existe deja.");
                ViewBag.EstSysteme = estSysteme;
                ViewBag.RoleId = role.Id;
                ViewBag.RoleName = role.Name;
                return View(dto);
            }

            role.Name = nomTechnique;
            role.NormalizedName = nomTechnique.ToUpperInvariant();
            var updateResult = await roleManager.UpdateAsync(role);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                ViewBag.EstSysteme = estSysteme;
                ViewBag.RoleId = role.Id;
                ViewBag.RoleName = role.Name;
                return View(dto);
            }
        }

        var meta = await db.RolesMetadonnees.FirstOrDefaultAsync(m => m.RoleId == id);
        if (meta is null)
        {
            meta = new RoleMetadonnee
            {
                Id = Guid.NewGuid(),
                RoleId = id,
                EstSysteme = estSysteme
            };
            db.RolesMetadonnees.Add(meta);
        }

        meta.Libelle = dto.Libelle?.Trim() ?? role.Name!;
        meta.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        meta.Visibilite = string.IsNullOrWhiteSpace(dto.Visibilite) ? null : dto.Visibilite.Trim();
        meta.Hierarchie = dto.Hierarchie;

        await db.SaveChangesAsync();
        TempData["Success"] = $"Role \"{meta.Libelle}\" mis a jour.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var role = await roleManager.FindByIdAsync(id.ToString());
        if (role is null || role.Name is null)
        {
            return NotFound();
        }

        if (RoleNames.Definitions.Any(d => d.Name == role.Name))
        {
            TempData["Error"] = "Les roles systeme ne peuvent pas etre supprimes.";
            return RedirectToAction(nameof(Index));
        }

        var hasUsers = await db.UserRoles.AnyAsync(ur => ur.RoleId == id);
        if (hasUsers)
        {
            TempData["Error"] = "Ce role est attribue a des utilisateurs. Retirez-le ou desactivez-le avant de le supprimer.";
            return RedirectToAction(nameof(Index));
        }

        var meta = await EnsureMetadonneeAsync(role);
        meta.EstSupprime = true;
        meta.EstActif = false;
        meta.DateSuppression = DateTime.UtcNow;
        await db.SaveChangesAsync();

        TempData["Success"] = $"Role \"{meta.Libelle}\" supprime (suppression logique).";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restaurer(Guid id)
    {
        var role = await roleManager.FindByIdAsync(id.ToString());
        if (role is null || role.Name is null)
        {
            return NotFound();
        }

        var meta = await db.RolesMetadonnees.FirstOrDefaultAsync(m => m.RoleId == id);
        if (meta is null || !meta.EstSupprime)
        {
            TempData["Error"] = "Ce role n'est pas supprime.";
            return RedirectToAction(nameof(Index), new { inclureSupprimes = true });
        }

        meta.EstSupprime = false;
        meta.EstActif = true;
        meta.DateSuppression = null;
        await db.SaveChangesAsync();

        TempData["Success"] = $"Role \"{meta.Libelle}\" restaure.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desactiver(Guid id)
    {
        var role = await roleManager.FindByIdAsync(id.ToString());
        if (role is null || role.Name is null)
        {
            return NotFound();
        }

        if (string.Equals(role.Name, RoleNames.Administrateur, StringComparison.Ordinal))
        {
            TempData["Error"] = "Le role Administrateur ne peut pas etre desactive.";
            return RedirectToAction(nameof(Index));
        }

        var meta = await EnsureMetadonneeAsync(role);
        if (meta.EstSupprime)
        {
            TempData["Error"] = "Ce role est supprime. Restaurez-le d'abord.";
            return RedirectToAction(nameof(Index));
        }

        meta.EstActif = false;
        await db.SaveChangesAsync();

        TempData["Success"] = $"Role \"{meta.Libelle}\" desactive. Les utilisateurs n'auront plus les permissions associees.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactiver(Guid id)
    {
        var role = await roleManager.FindByIdAsync(id.ToString());
        if (role is null || role.Name is null)
        {
            return NotFound();
        }

        var meta = await EnsureMetadonneeAsync(role);
        if (meta.EstSupprime)
        {
            TempData["Error"] = "Ce role est supprime. Restaurez-le d'abord.";
            return RedirectToAction(nameof(Index));
        }

        meta.EstActif = true;
        await db.SaveChangesAsync();

        TempData["Success"] = $"Role \"{meta.Libelle}\" reactive.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<RoleMetadonnee> EnsureMetadonneeAsync(IdentityRole<Guid> role)
    {
        var meta = await db.RolesMetadonnees.FirstOrDefaultAsync(m => m.RoleId == role.Id);
        if (meta is not null)
        {
            return meta;
        }

        var definition = RoleNames.Definitions.FirstOrDefault(d => d.Name == role.Name);
        var estSysteme = definition is not null;

        meta = new RoleMetadonnee
        {
            Id = Guid.NewGuid(),
            RoleId = role.Id,
            Libelle = definition?.Label ?? role.Name!,
            Description = definition?.Description,
            Visibilite = definition?.Visibility,
            Hierarchie = definition?.Hierarchy ?? 50,
            EstSysteme = estSysteme,
            EstActif = true,
            EstSupprime = false
        };
        db.RolesMetadonnees.Add(meta);
        return meta;
    }

    private static string NormaliserNomTechnique(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var nettoye = value.Trim();
        var chars = nettoye.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray();
        return new string(chars);
    }
}
