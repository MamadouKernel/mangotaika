using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Models;
using MangoTaika.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Diagnostics;

namespace MangoTaika.Controllers;

public class HomeController(AppDbContext db, IConfiguration configuration, IMobilePaymentGateway mobilePaymentGateway) : Controller
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

    public IActionResult Contact() => View(new Data.Entities.ContactMessage());

    [HttpGet]
    public async Task<IActionResult> FaireUnDon()
    {
        ViewBag.ComptePaiement = await db.ComptesPaiementMobile
            .Where(c => c.EstActif && !c.EstSupprime)
            .OrderByDescending(c => c.EstPrincipal)
            .ThenBy(c => c.Libelle)
            .FirstOrDefaultAsync();

        return View(new DonPublic());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FaireUnDon(DonPublic model)
    {
        ViewBag.ComptePaiement = await db.ComptesPaiementMobile
            .Where(c => c.EstActif && !c.EstSupprime)
            .OrderByDescending(c => c.EstPrincipal)
            .ThenBy(c => c.Libelle)
            .FirstOrDefaultAsync();

        if (model.Montant <= 0)
        {
            ModelState.AddModelError(nameof(model.Montant), "Le montant du don doit etre superieur a 0.");
        }

        if (string.IsNullOrWhiteSpace(model.NomDonateur))
        {
            ModelState.AddModelError(nameof(model.NomDonateur), "Le nom du donateur est obligatoire.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        model.Id = Guid.NewGuid();
        model.NomDonateur = model.NomDonateur.Trim();
        model.Telephone = string.IsNullOrWhiteSpace(model.Telephone) ? null : model.Telephone.Trim();
        model.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
        model.Devise = string.IsNullOrWhiteSpace(model.Devise) ? "XOF" : model.Devise.Trim().ToUpperInvariant();
        model.ReferencePaiement = string.IsNullOrWhiteSpace(model.ReferencePaiement) ? null : model.ReferencePaiement.Trim();
        model.Message = string.IsNullOrWhiteSpace(model.Message) ? null : model.Message.Trim();
        model.Statut = StatutDonPublic.Declare;
        model.RecuToken = Guid.NewGuid().ToString("N");
        var payment = await mobilePaymentGateway.CreateRequestAsync(new MobilePaymentRequest(
            "Don",
            model.NomDonateur,
            model.Telephone,
            model.Email,
            model.Montant,
            model.Devise,
            $"DON-{model.Id:N}",
            model.ReferencePaiement));
        model.ReferencePaiement = payment.ProviderReference ?? model.ReferencePaiement;
        db.DonsPublics.Add(model);
        await db.SaveChangesAsync();

        TempData["Success"] = $"Merci. Votre intention de don a ete enregistree. {payment.Message}";
        return RedirectToAction(nameof(FaireUnDon));
    }

    [HttpGet]
    public IActionResult Verification() => View(new VerificationScoutViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Verification(VerificationScoutViewModel model)
    {
        model.RechercheEffectuee = true;

        var hasMatricule = !string.IsNullOrWhiteSpace(model.Matricule);
        var hasIdentity = !string.IsNullOrWhiteSpace(model.Nom) && !string.IsNullOrWhiteSpace(model.Prenom);

        if (!hasMatricule && !hasIdentity)
        {
            model.Statut = "Inexistant";
            ModelState.AddModelError(string.Empty, "Renseignez soit le matricule, soit le nom et un prenom pour lancer la verification.");
            return View(model);
        }

        var matricule = model.Matricule?.Trim().ToLowerInvariant();
        var scout = hasMatricule
            ? await db.Scouts
                .AsNoTracking()
                .Include(s => s.Groupe)
                .Include(s => s.Branche)
                .FirstOrDefaultAsync(s => s.IsActive && s.Matricule != null && s.Matricule.ToLower() == matricule)
            : (await db.Scouts
                .AsNoTracking()
                .Include(s => s.Groupe)
                .Include(s => s.Branche)
                .Where(s => s.IsActive)
                .ToListAsync())
                .FirstOrDefault(s => IsIdentityMatch(s, model.Nom!, model.Prenom!));

        if (scout == null)
        {
            model.Statut = "Inexistant";
            return View(model);
        }

        var currentYear = DateTime.UtcNow.Year;
        var inscription = await db.InscriptionsAnnuellesScouts
            .AsNoTracking()
            .Where(i => i.ScoutId == scout.Id && i.AnneeReference == currentYear)
            .OrderByDescending(i => i.DateInscription)
            .FirstOrDefaultAsync();

        model.NomComplet = $"{scout.Prenom} {scout.Nom}";
        model.MatriculeTrouve = scout.Matricule;
        model.Groupe = scout.Groupe?.Nom;
        model.Branche = scout.Branche?.Nom;
        model.Statut = inscription?.CotisationNationaleAjour == true ? "A jour" : "Non à jour";

        return View(model);
    }

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

    public IActionResult Avis() => View(new Data.Entities.ContactMessage());

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
        await LoadLivreDorHistoriqueAsync();
        return View(await LoadLivreDorMessagesAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LivreDor(Data.Entities.LivreDor model, string? Website)
    {
        if (!string.IsNullOrEmpty(Website)) return RedirectToAction(nameof(LivreDor));

        if (!ModelState.IsValid)
        {
            ViewBag.FormNomAuteur = model.NomAuteur;
            ViewBag.FormMessage = model.Message;
            await LoadLivreDorHistoriqueAsync();
            return View(await LoadLivreDorMessagesAsync());
        }
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

    private Task<List<Data.Entities.LivreDor>> LoadLivreDorMessagesAsync()
    {
        return db.LivreDor
            .Where(l => l.EstValide && !l.EstSupprime)
            .OrderByDescending(l => l.DateCreation)
            .ToListAsync();
    }

    private async Task LoadLivreDorHistoriqueAsync()
    {
        ViewBag.Historique = await db.MembresHistoriques
            .Where(m => !m.EstSupprime)
            .OrderBy(m => m.Ordre)
            .ThenBy(m => m.Nom)
            .ToListAsync();
    }


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

    private static string NormalizeSearchValue(string value)
    {
        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var chars = normalized
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray();
        return new string(chars).Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    private static bool IsIdentityMatch(Data.Entities.Scout scout, string nom, string prenom)
    {
        var normalizedNom = NormalizeSearchValue(nom);
        var normalizedPrenom = NormalizeSearchValue(prenom);
        var scoutPrenoms = NormalizeSearchValue(scout.Prenom)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return NormalizeSearchValue(scout.Nom) == normalizedNom
            && (NormalizeSearchValue(scout.Prenom) == normalizedPrenom || scoutPrenoms.Contains(normalizedPrenom));
    }
}
