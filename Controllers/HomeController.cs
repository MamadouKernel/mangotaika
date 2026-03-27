using MangoTaika.Data;
using MangoTaika.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace MangoTaika.Controllers;

public class HomeController(AppDbContext db, IConfiguration configuration) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.MotCommissaire = await db.MotsCommissaire
            .Where(m => m.EstActif && !m.EstSupprime)
            .OrderByDescending(m => m.Annee)
            .FirstOrDefaultAsync();

        ViewBag.DerniereActualite = await db.Actualites
            .Where(a => a.EstPublie && !a.EstSupprime && a.ImageUrl != null)
            .OrderByDescending(a => a.DatePublication)
            .FirstOrDefaultAsync();

        ViewBag.Galeries = await db.Galeries
            .Where(g => g.EstPublie && !g.EstSupprime)
            .OrderByDescending(g => g.DateUpload)
            .Take(12)
            .ToListAsync();

        var groupes = await db.Groupes
            .Include(g => g.Responsable)
            .Include(g => g.Branches).ThenInclude(b => b.ChefUnite)
            .Where(g => g.IsActive && g.Latitude != null && g.Longitude != null)
            .ToListAsync();

        ViewBag.Groupes = groupes;

        // Partenaires actifs
        ViewBag.Partenaires = await db.Partenaires
            .Where(p => p.EstActif && !p.EstSupprime)
            .OrderBy(p => p.Ordre)
            .ToListAsync();

        // Liens réseaux sociaux (pour footer)
        ViewBag.LiensRS = await db.LiensReseauxSociaux
            .Where(l => l.EstActif)
            .OrderBy(l => l.Ordre)
            .ToListAsync();

        // JSON pour la carte (évite les problèmes de locale avec les décimales)
        ViewBag.GroupesJson = System.Text.Json.JsonSerializer.Serialize(groupes.Select(g => new
        {
            nom = g.Nom,
            adresse = g.Adresse ?? "",
            lat = g.Latitude,
            lng = g.Longitude,
            chefGroupe = !string.IsNullOrWhiteSpace(g.NomChefGroupe)
                ? g.NomChefGroupe
                : (g.Responsable != null ? $"{g.Responsable.Prenom} {g.Responsable.Nom}" : "Non renseigné"),
            branches = g.Branches.Where(b => b.IsActive).Select(b => new
            {
                nom = b.Nom,
                ageMin = b.AgeMin ?? 0,
                ageMax = b.AgeMax ?? 99,
                chef = b.ChefUnite != null ? $"{b.ChefUnite.Prenom} {b.ChefUnite.Nom}" : (b.NomChefUnite ?? "")
            })
        }));

        return View();
    }

    public IActionResult Contact() => View();

    public IActionResult WhatsApp()
    {
        var normalizedPhone = (configuration["Contact:WhatsAppNumber"] ?? "2250759013291")
            .Replace("+", string.Empty)
            .Replace(" ", string.Empty);

        var options = new List<WhatsAppContactOptionViewModel>
        {
            CreateWhatsAppOption(
                normalizedPhone,
                "Informations generales",
                "Pour toute demande d'information, d'orientation, d'accompagnement ou de partenariat.",
                "Bonjour et bienvenue au District Scout MANGO TAIKA. Nous vous remercions de l'interet que vous portez a notre mouvement. Nous restons a votre disposition pour toute demande d'information, d'orientation, d'accompagnement ou de partenariat. Merci de nous preciser votre besoin afin que nous puissions vous repondre dans les meilleurs delais.",
                "bi-building"),
            CreateWhatsAppOption(
                normalizedPhone,
                "Parents et familles",
                "Pour les parents et familles souhaitant mieux comprendre la vie de groupe et l'accompagnement propose.",
                "Bonjour et bienvenue au District Scout MANGO TAIKA. Nous serons heureux de vous accompagner pour toute demande relative a l'inscription d'un enfant, a la vie de groupe, aux activites ou au parcours scout. Merci de nous indiquer votre besoin ainsi que l'age de l'enfant concerne afin de mieux vous orienter.",
                "bi-people"),
            CreateWhatsAppOption(
                normalizedPhone,
                "Inscriptions",
                "Pour toute demande d'inscription ou de pre-inscription au sein du district.",
                "Bonjour et merci pour l'interet que vous portez au District Scout MANGO TAIKA. Pour toute demande d'inscription ou de pre-inscription, merci de nous communiquer votre commune, l'age du scout ou de la scoute concerne(e), ainsi que toute information utile afin que nous puissions vous orienter vers le groupe le plus adapte.",
                "bi-person-plus")
        };

        return View(new WhatsAppContactViewModel
        {
            DisplayPhoneNumber = $"+{normalizedPhone}",
            Options = options
        });
    }

    public async Task<IActionResult> Galerie()
    {
        var images = await db.Galeries
            .Where(g => g.EstPublie && !g.EstSupprime)
            .OrderByDescending(g => g.DateUpload)
            .ToListAsync();
        return View(images);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(Data.Entities.ContactMessage model, string? Website)
    {
        // Honeypot anti-spam : si le champ caché est rempli, c'est un bot
        if (!string.IsNullOrEmpty(Website)) return RedirectToAction(nameof(Contact));

        if (!ModelState.IsValid) return View(model);
        model.Id = Guid.NewGuid();
        model.Type = "Contact";
        db.ContactMessages.Add(model);
        await db.SaveChangesAsync();
        TempData["Success"] = "Votre message a été envoyé avec succès.";
        return RedirectToAction(nameof(Contact));
    }

    public IActionResult Avis() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Avis(Data.Entities.ContactMessage model, string? Website)
    {
        if (!string.IsNullOrEmpty(Website)) return RedirectToAction(nameof(Avis));

        if (!ModelState.IsValid) return View(model);
        model.Id = Guid.NewGuid();
        model.Type = "Avis";
        db.ContactMessages.Add(model);
        await db.SaveChangesAsync();
        TempData["Success"] = "Merci pour votre avis. Il a bien été enregistré.";
        return RedirectToAction(nameof(Avis));
    }

    public async Task<IActionResult> LivreDor()
    {
        var messages = await db.LivreDor
            .Where(l => l.EstValide && !l.EstSupprime)
            .OrderByDescending(l => l.DateCreation)
            .ToListAsync();

        // Premières pages : anciens commissaires, CG, membres CAD
        ViewBag.Historique = await db.MembresHistoriques
            .Where(m => !m.EstSupprime)
            .OrderBy(m => m.Categorie)
            .ThenBy(m => m.Ordre)
            .ToListAsync();

        return View(messages);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LivreDor(Data.Entities.LivreDor model, string? Website)
    {
        if (!string.IsNullOrEmpty(Website)) return RedirectToAction(nameof(LivreDor));

        if (!ModelState.IsValid) return View();
        model.Id = Guid.NewGuid();
        db.LivreDor.Add(model);
        await db.SaveChangesAsync();
        TempData["Success"] = "Votre message sera publié après validation.";
        return RedirectToAction(nameof(LivreDor));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public IActionResult PolitiqueConfidentialite() => View();

    private static WhatsAppContactOptionViewModel CreateWhatsAppOption(
        string phoneNumber,
        string title,
        string description,
        string message,
        string iconClass)
    {
        return new WhatsAppContactOptionViewModel
        {
            Title = title,
            Description = description,
            Message = message,
            IconClass = iconClass,
            Url = $"https://wa.me/{phoneNumber}?text={Uri.EscapeDataString(message)}"
        };
    }
}
