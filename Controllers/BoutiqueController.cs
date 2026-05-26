using ClosedXML.Excel;
using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Helpers;
using MangoTaika.Models;
using MangoTaika.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace MangoTaika.Controllers;

public class BoutiqueController(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IFileUploadService fileUpload,
    IEmailNotificationService emailService,
    IMobilePaymentGateway mobilePaymentGateway,
    IConfiguration configuration) : Controller
{
    private const string CartSessionKey = "BoutiqueCart";

    [AllowAnonymous]
    public async Task<IActionResult> Index(
        string? q,
        string? categorie,
        decimal? prixMin,
        decimal? prixMax,
        bool disponible = false,
        string tri = "recent")
    {
        var query = db.ArticlesBoutique
            .Where(a => a.EstPublie && !a.EstSupprime)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(a =>
                a.Nom.ToLower().Contains(term)
                || (a.Description != null && a.Description.ToLower().Contains(term))
                || (a.Categorie != null && a.Categorie.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(categorie))
        {
            var selectedCategory = categorie.Trim();
            query = query.Where(a => a.Categorie == selectedCategory);
        }

        if (prixMin.HasValue && prixMin.Value > 0)
        {
            query = query.Where(a => a.Prix >= prixMin.Value);
        }

        if (prixMax.HasValue && prixMax.Value > 0)
        {
            query = query.Where(a => a.Prix <= prixMax.Value);
        }

        if (disponible)
        {
            query = query.Where(a => a.StockDisponible > 0);
        }

        ViewBag.Query = q;
        ViewBag.Categorie = categorie;
        ViewBag.PrixMin = prixMin;
        ViewBag.PrixMax = prixMax;
        ViewBag.Disponible = disponible;
        ViewBag.Tri = tri;
        ViewBag.PanierCount = GetCart().Sum(i => i.Quantite);
        ViewBag.TotalArticles = await db.ArticlesBoutique.CountAsync(a => a.EstPublie && !a.EstSupprime);
        ViewBag.ArticleCount = await query.CountAsync();
        ViewBag.CategoryCounts = await db.ArticlesBoutique
            .Where(a => a.EstPublie && !a.EstSupprime && a.Categorie != null && a.Categorie != "")
            .GroupBy(a => a.Categorie!)
            .Select(g => new BoutiqueCategoryFilter { Categorie = g.Key, Count = g.Count() })
            .OrderBy(c => c.Categorie)
            .ToListAsync();
        ViewBag.Categories = await db.ArticlesBoutique
            .Where(a => a.EstPublie && !a.EstSupprime && a.Categorie != null && a.Categorie != "")
            .Select(a => a.Categorie!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        query = tri switch
        {
            "prix-asc" => query.OrderBy(a => a.Prix).ThenBy(a => a.Nom),
            "prix-desc" => query.OrderByDescending(a => a.Prix).ThenBy(a => a.Nom),
            "stock" => query.OrderByDescending(a => a.StockDisponible).ThenBy(a => a.Nom),
            "nom" => query.OrderBy(a => a.Nom),
            _ => query.OrderByDescending(a => a.DateCreation).ThenBy(a => a.Nom)
        };

        var articles = await query.ToListAsync();
        return View(articles);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(Guid id)
    {
        var article = await db.ArticlesBoutique
            .FirstOrDefaultAsync(a => a.Id == id && a.EstPublie && !a.EstSupprime);

        ViewBag.PanierCount = GetCart().Sum(i => i.Quantite);
        return article is null ? NotFound() : View(article);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AjouterAuPanier(Guid articleId, int quantite = 1, string? returnUrl = null, bool checkout = false)
    {
        var article = await db.ArticlesBoutique
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == articleId && a.EstPublie && !a.EstSupprime);
        if (article is null)
        {
            TempData["Error"] = "Article introuvable ou retire de la boutique. Choisissez un autre article disponible.";
            return RedirectToAction(nameof(Index));
        }

        quantite = Math.Max(1, quantite);
        var cart = GetCart();
        var existing = cart.FirstOrDefault(i => i.ArticleId == articleId);
        var requestedQuantity = (existing?.Quantite ?? 0) + quantite;
        if (article.StockDisponible > 0 && requestedQuantity > article.StockDisponible)
        {
            TempData["Error"] = "La quantite demandee depasse le stock disponible.";
            return RedirectToSafeUrl(returnUrl, nameof(Details), new { id = articleId });
        }

        if (existing is null)
        {
            cart.Add(new BoutiqueCartItem { ArticleId = articleId, Quantite = quantite });
        }
        else
        {
            existing.Quantite = requestedQuantity;
        }

        SaveCart(cart);
        TempData["Success"] = $"{article.Nom} ajoute au panier.";
        return checkout
            ? RedirectToAction(nameof(Panier))
            : RedirectToSafeUrl(returnUrl, nameof(Index));
    }

    [AllowAnonymous]
    public async Task<IActionResult> Panier()
    {
        var cart = await BuildCartViewModelAsync();
        ViewBag.PanierCount = cart.NombreArticles;
        return View(cart);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ModifierPanier(Guid articleId, int quantite)
    {
        var cart = GetCart();
        var item = cart.FirstOrDefault(i => i.ArticleId == articleId);
        if (item is null) return RedirectToAction(nameof(Panier));

        if (quantite <= 0)
        {
            cart.Remove(item);
            SaveCart(cart);
            TempData["Success"] = "Article retire du panier.";
            return RedirectToAction(nameof(Panier));
        }

        var article = await db.ArticlesBoutique
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == articleId && a.EstPublie && !a.EstSupprime);
        if (article is null)
        {
            cart.Remove(item);
            SaveCart(cart);
            TempData["Error"] = "Cet article n'est plus disponible.";
            return RedirectToAction(nameof(Panier));
        }

        if (article.StockDisponible > 0 && quantite > article.StockDisponible)
        {
            TempData["Error"] = "La quantite demandee depasse le stock disponible.";
            return RedirectToAction(nameof(Panier));
        }

        item.Quantite = quantite;
        SaveCart(cart);
        TempData["Success"] = "Panier mis a jour.";
        return RedirectToAction(nameof(Panier));
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public IActionResult RetirerDuPanier(Guid articleId)
    {
        var cart = GetCart();
        cart.RemoveAll(i => i.ArticleId == articleId);
        SaveCart(cart);
        TempData["Success"] = "Article retire du panier.";
        return RedirectToAction(nameof(Panier));
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public IActionResult ViderPanier()
    {
        ClearCart();
        TempData["Success"] = "Panier vide.";
        return RedirectToAction(nameof(Panier));
    }

    [AllowAnonymous]
    public async Task<IActionResult> Checkout()
    {
        var cart = await BuildCartViewModelAsync();
        if (!cart.Lignes.Any())
        {
            TempData["Error"] = "Votre panier est vide.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.PanierCount = cart.NombreArticles;
        return View(cart);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
    public async Task<IActionResult> Admin()
    {
        return View(await db.ArticlesBoutique
            .Where(a => !a.EstSupprime)
            .OrderBy(a => a.Nom)
            .ToListAsync());
    }

    [Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
    public IActionResult Create() => View(new ArticleBoutique { Devise = "XOF", EstPublie = true });

    [Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
    public IActionResult Import() => View();

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ArticleBoutique model, IFormFile? Image)
    {
        ValidateArticle(model);
        if (!ModelState.IsValid) return View(model);

        try
        {
            model.ImageUrl = Image is null ? NormalizeOptional(model.ImageUrl) : await fileUpload.SaveImageAsync(Image, "boutique");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.ImageUrl), ex.Message);
            return View(model);
        }

        model.Id = Guid.NewGuid();
        model.Nom = model.Nom.Trim();
        model.Categorie = NormalizeOptional(model.Categorie);
        model.Devise = NormalizeCurrency(model.Devise);
        db.ArticlesBoutique.Add(model);
        await db.SaveChangesAsync();
        TempData["Success"] = "Article ajoute a la boutique.";
        return RedirectToAction(nameof(Admin));
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(IFormFile? fichier, bool mettreAJourExistants = true)
    {
        if (fichier is null || fichier.Length == 0)
        {
            TempData["Error"] = "Selectionnez un fichier CSV ou Excel a importer.";
            return RedirectToAction(nameof(Import));
        }

        var extension = Path.GetExtension(fichier.FileName).ToLowerInvariant();
        if (extension is not ".csv" and not ".xlsx")
        {
            TempData["Error"] = "Format non supporte. Utilisez un fichier .csv ou .xlsx.";
            return RedirectToAction(nameof(Import));
        }

        List<ArticleBoutique> importedArticles;
        try
        {
            importedArticles = extension == ".xlsx"
                ? ReadArticlesFromExcel(fichier)
                : await ReadArticlesFromCsvAsync(fichier);
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Import));
        }

        if (importedArticles.Count == 0)
        {
            TempData["Error"] = "Aucun article valide n'a ete trouve dans le fichier.";
            return RedirectToAction(nameof(Import));
        }

        var created = 0;
        var updated = 0;
        var skipped = 0;
        var existingArticles = await db.ArticlesBoutique
            .Where(a => !a.EstSupprime)
            .ToDictionaryAsync(a => a.Nom.ToLower());

        foreach (var imported in importedArticles)
        {
            var key = imported.Nom.ToLower();
            if (existingArticles.TryGetValue(key, out var existing))
            {
                if (!mettreAJourExistants)
                {
                    skipped++;
                    continue;
                }

                existing.Categorie = imported.Categorie;
                existing.Description = imported.Description;
                existing.ImageUrl = imported.ImageUrl;
                existing.Prix = imported.Prix;
                existing.Devise = imported.Devise;
                existing.StockDisponible = imported.StockDisponible;
                existing.EstPublie = imported.EstPublie;
                existing.DateModification = DateTime.UtcNow;
                updated++;
                continue;
            }

            imported.Id = Guid.NewGuid();
            db.ArticlesBoutique.Add(imported);
            existingArticles[key] = imported;
            created++;
        }

        await db.SaveChangesAsync();
        TempData["Success"] = $"Import termine : {created} article(s) cree(s), {updated} mis a jour, {skipped} ignore(s).";
        return RedirectToAction(nameof(Admin));
    }

    [Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var article = await db.ArticlesBoutique.FirstOrDefaultAsync(a => a.Id == id && !a.EstSupprime);
        return article is null ? NotFound() : View(article);
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ArticleBoutique model, IFormFile? Image)
    {
        var article = await db.ArticlesBoutique.FirstOrDefaultAsync(a => a.Id == id && !a.EstSupprime);
        if (article is null) return NotFound();

        ValidateArticle(model);
        if (!ModelState.IsValid)
        {
            model.Id = id;
            return View(model);
        }

        try
        {
            article.ImageUrl = Image is null ? NormalizeOptional(model.ImageUrl) : await fileUpload.SaveImageAsync(Image, "boutique");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.ImageUrl), ex.Message);
            model.Id = id;
            return View(model);
        }

        article.Nom = model.Nom.Trim();
        article.Categorie = NormalizeOptional(model.Categorie);
        article.Description = NormalizeOptional(model.Description);
        article.Prix = model.Prix;
        article.Devise = NormalizeCurrency(model.Devise);
        article.StockDisponible = model.StockDisponible;
        article.EstPublie = model.EstPublie;
        article.DateModification = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["Success"] = "Article mis a jour.";
        return RedirectToAction(nameof(Admin));
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var article = await db.ArticlesBoutique.FirstOrDefaultAsync(a => a.Id == id && !a.EstSupprime);
        if (article is null) return NotFound();

        article.EstSupprime = true;
        article.EstPublie = false;
        article.DateModification = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["Success"] = "Article retire de la boutique.";
        return RedirectToAction(nameof(Admin));
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Commander(
        Guid articleId,
        string nomClient,
        string telephoneClient,
        string? emailClient,
        string? referencePaiement,
        string? modePaiement,
        int quantite = 1)
    {
        var article = await db.ArticlesBoutique
            .FirstOrDefaultAsync(a => a.Id == articleId && a.EstPublie && !a.EstSupprime);

        if (article is null) return NotFound();
        if (quantite <= 0) quantite = 1;
        if (article.StockDisponible > 0 && quantite > article.StockDisponible)
        {
            TempData["Error"] = $"Stock insuffisant : {article.StockDisponible} article(s) disponible(s), quantite demandee {quantite}. Reduisez la quantite.";
            return RedirectToAction(nameof(Details), new { id = articleId });
        }

        if (string.IsNullOrWhiteSpace(nomClient) || string.IsNullOrWhiteSpace(telephoneClient))
        {
            TempData["Error"] = "Commande incomplete : renseignez le nom du client et un telephone joignable.";
            return RedirectToAction(nameof(Details), new { id = articleId });
        }

        var user = User.Identity?.IsAuthenticated == true ? await userManager.GetUserAsync(User) : null;
        var paymentMode = string.Equals(modePaiement, "PaiementLivraison", StringComparison.OrdinalIgnoreCase)
            ? ModePaiementCommandeBoutique.PaiementLivraison
            : ModePaiementCommandeBoutique.MobileMoney;
        var commande = new CommandeBoutique
        {
            Id = Guid.NewGuid(),
            ClientId = user?.Id,
            NomClient = nomClient.Trim(),
            TelephoneClient = telephoneClient.Trim(),
            EmailClient = NormalizeOptional(emailClient),
            ReferencePaiement = NormalizeOptional(referencePaiement),
            ModePaiement = paymentMode,
            RecuToken = Guid.NewGuid().ToString("N"),
            Total = article.Prix * quantite,
            Devise = article.Devise,
            Lignes =
            [
                new LigneCommandeBoutique
                {
                    Id = Guid.NewGuid(),
                    ArticleBoutiqueId = article.Id,
                    Quantite = quantite,
                    PrixUnitaire = article.Prix
                }
            ]
        };

        var paymentMessage = "Commande recue. Paiement attendu a la livraison.";
        if (paymentMode == ModePaiementCommandeBoutique.MobileMoney)
        {
            var payment = await mobilePaymentGateway.CreateRequestAsync(new MobilePaymentRequest(
                "CommandeBoutique",
                commande.NomClient,
                commande.TelephoneClient,
                commande.EmailClient,
                commande.Total,
                commande.Devise,
                $"CMD-{commande.Id:N}",
                commande.ReferencePaiement));
            commande.ReferencePaiement = payment.ProviderReference ?? commande.ReferencePaiement;
            paymentMessage = payment.Message;
        }
        else
        {
            commande.ReferencePaiement = "Paiement a la livraison";
        }

        db.CommandesBoutique.Add(commande);
        await db.SaveChangesAsync();
        await NotifyCustomerAsync(
            commande,
            "Commande boutique recue",
            $"Votre commande {article.Nom} a ete enregistree. Statut : En attente. Mode de paiement : {FormatPaymentMode(commande.ModePaiement)}. Montant : {commande.Total:N0} {commande.Devise}.",
            includeReceiptLink: false);

        TempData["Success"] = $"Commande enregistree. {paymentMessage}";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(
        string nomClient,
        string telephoneClient,
        string? emailClient,
        string? referencePaiement,
        string? modePaiement)
    {
        var cart = await BuildCartViewModelAsync();
        if (!cart.Lignes.Any())
        {
            TempData["Error"] = "Votre panier est vide. Ajoutez au moins un article avant de passer la commande.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(nomClient) || string.IsNullOrWhiteSpace(telephoneClient))
        {
            TempData["Error"] = "Commande incomplete : renseignez le nom du client et un telephone joignable.";
            return RedirectToAction(nameof(Checkout));
        }

        var mixedCurrencies = cart.Lignes.Select(l => l.Article.Devise).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1;
        if (mixedCurrencies)
        {
            TempData["Error"] = "Le panier contient plusieurs devises. Creez une commande separee pour chaque devise.";
            return RedirectToAction(nameof(Panier));
        }

        foreach (var line in cart.Lignes)
        {
            if (line.Article.StockDisponible > 0 && line.Quantite > line.Article.StockDisponible)
            {
                TempData["Error"] = $"Stock insuffisant pour : {line.Article.Nom}.";
                return RedirectToAction(nameof(Panier));
            }
        }

        var user = User.Identity?.IsAuthenticated == true ? await userManager.GetUserAsync(User) : null;
        var paymentMode = string.Equals(modePaiement, "PaiementLivraison", StringComparison.OrdinalIgnoreCase)
            ? ModePaiementCommandeBoutique.PaiementLivraison
            : ModePaiementCommandeBoutique.MobileMoney;
        var commande = new CommandeBoutique
        {
            Id = Guid.NewGuid(),
            ClientId = user?.Id,
            NomClient = nomClient.Trim(),
            TelephoneClient = telephoneClient.Trim(),
            EmailClient = NormalizeOptional(emailClient),
            ReferencePaiement = NormalizeOptional(referencePaiement),
            ModePaiement = paymentMode,
            RecuToken = Guid.NewGuid().ToString("N"),
            Total = cart.Total,
            Devise = cart.Devise,
            Lignes = cart.Lignes.Select(line => new LigneCommandeBoutique
            {
                Id = Guid.NewGuid(),
                ArticleBoutiqueId = line.Article.Id,
                Quantite = line.Quantite,
                PrixUnitaire = line.Article.Prix
            }).ToList()
        };

        var paymentMessage = "Commande recue. Paiement attendu a la livraison.";
        if (paymentMode == ModePaiementCommandeBoutique.MobileMoney)
        {
            var payment = await mobilePaymentGateway.CreateRequestAsync(new MobilePaymentRequest(
                "CommandeBoutique",
                commande.NomClient,
                commande.TelephoneClient,
                commande.EmailClient,
                commande.Total,
                commande.Devise,
                $"CMD-{commande.Id:N}",
                commande.ReferencePaiement));
            commande.ReferencePaiement = payment.ProviderReference ?? commande.ReferencePaiement;
            paymentMessage = payment.Message;
        }
        else
        {
            commande.ReferencePaiement = "Paiement a la livraison";
        }

        db.CommandesBoutique.Add(commande);
        await db.SaveChangesAsync();
        ClearCart();

        var articleSummary = string.Join(", ", cart.Lignes.Select(l => $"{l.Article.Nom} x{l.Quantite}"));
        await NotifyCustomerAsync(
            commande,
            "Commande boutique recue",
            $"Votre commande boutique a ete enregistree. Articles : {articleSummary}. Statut : En attente. Mode de paiement : {FormatPaymentMode(commande.ModePaiement)}. Montant : {commande.Total:N0} {commande.Devise}.",
            includeReceiptLink: false);

        TempData["Success"] = $"Commande panier enregistree. {paymentMessage}";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
    public async Task<IActionResult> Commandes(StatutCommandeBoutique? statut)
    {
        var query = db.CommandesBoutique
            .Include(c => c.Lignes).ThenInclude(l => l.ArticleBoutique)
            .Include(c => c.TraitePar)
            .AsNoTracking()
            .AsQueryable();

        if (statut.HasValue)
        {
            query = query.Where(c => c.Statut == statut.Value);
        }

        ViewBag.Statut = statut;
        return View(await query.OrderByDescending(c => c.DateCreation).ToListAsync());
    }

    [Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
    public async Task<IActionResult> CommandeDetails(Guid id)
    {
        var commande = await LoadCommandeAsync(id, asNoTracking: true);
        return commande is null ? NotFound() : View(commande);
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmerCommande(Guid id, string? commentaireTraitement)
    {
        var commande = await LoadCommandeAsync(id);
        if (commande is null) return NotFound();

        if (commande.Statut is StatutCommandeBoutique.Payee or StatutCommandeBoutique.Livree)
        {
            TempData["Info"] = "Cette commande est deja confirmee.";
            return RedirectToAction(nameof(CommandeDetails), new { id });
        }

        var missingStock = commande.Lignes
            .Where(l => l.ArticleBoutique.StockDisponible > 0 && l.Quantite > l.ArticleBoutique.StockDisponible)
            .Select(l => l.ArticleBoutique.Nom)
            .ToList();
        if (missingStock.Count > 0)
        {
            TempData["Error"] = "Stock insuffisant pour : " + string.Join(", ", missingStock);
            return RedirectToAction(nameof(CommandeDetails), new { id });
        }

        foreach (var ligne in commande.Lignes)
        {
            if (ligne.ArticleBoutique.StockDisponible > 0)
            {
                ligne.ArticleBoutique.StockDisponible -= ligne.Quantite;
                ligne.ArticleBoutique.DateModification = DateTime.UtcNow;
            }
        }

        await MarkCommandeAsync(commande, StatutCommandeBoutique.Payee, commentaireTraitement);
        commande.NumeroRecu ??= $"CMD-{DateTime.UtcNow:yyyyMMdd}-{commande.Id.ToString("N")[..8].ToUpperInvariant()}";
        await db.SaveChangesAsync();

        await NotifyCustomerAsync(
            commande,
            "Commande boutique confirmee",
            $"Votre commande boutique est confirmee. Statut : Payee. Montant : {commande.Total:N0} {commande.Devise}.",
            includeReceiptLink: true);

        TempData["Success"] = "Commande confirmee, stock mis a jour et email envoye si une adresse est renseignee.";
        return RedirectToAction(nameof(CommandeDetails), new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarquerLivree(Guid id, string? commentaireTraitement)
    {
        var commande = await LoadCommandeAsync(id);
        if (commande is null) return NotFound();

        if (commande.Statut != StatutCommandeBoutique.Payee)
        {
            TempData["Error"] = "Seule une commande confirmee/payee peut etre marquee comme livree.";
            return RedirectToAction(nameof(CommandeDetails), new { id });
        }

        await MarkCommandeAsync(commande, StatutCommandeBoutique.Livree, commentaireTraitement);
        await db.SaveChangesAsync();
        await NotifyCustomerAsync(
            commande,
            "Commande boutique livree",
            $"Votre commande boutique a ete marquee comme livree. Statut : Livree.",
            includeReceiptLink: true);

        TempData["Success"] = "Commande marquee comme livree.";
        return RedirectToAction(nameof(CommandeDetails), new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AnnulerCommande(Guid id, string? commentaireTraitement)
    {
        var commande = await LoadCommandeAsync(id);
        if (commande is null) return NotFound();

        if (commande.Statut == StatutCommandeBoutique.Livree)
        {
            TempData["Error"] = "Une commande livree ne peut pas etre annulee depuis cet ecran.";
            return RedirectToAction(nameof(CommandeDetails), new { id });
        }

        if (commande.Statut == StatutCommandeBoutique.Payee)
        {
            foreach (var ligne in commande.Lignes)
            {
                ligne.ArticleBoutique.StockDisponible += ligne.Quantite;
                ligne.ArticleBoutique.DateModification = DateTime.UtcNow;
            }
        }

        await MarkCommandeAsync(commande, StatutCommandeBoutique.Annulee, commentaireTraitement);
        await db.SaveChangesAsync();
        await NotifyCustomerAsync(
            commande,
            "Commande boutique annulee",
            $"Votre commande boutique a ete annulee. Statut : Annulee. {commande.CommentaireTraitement ?? string.Empty}",
            includeReceiptLink: false);

        TempData["Success"] = "Commande annulee.";
        return RedirectToAction(nameof(CommandeDetails), new { id });
    }

    [Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
    public async Task<IActionResult> Recu(Guid id)
    {
        var commande = await LoadCommandeAsync(id, asNoTracking: true);
        if (commande is null) return NotFound();
        if (commande.Statut is not (StatutCommandeBoutique.Payee or StatutCommandeBoutique.Livree))
        {
            TempData["Error"] = "Le recu est disponible uniquement apres confirmation de la commande.";
            return RedirectToAction(nameof(CommandeDetails), new { id });
        }

        return BuildReceiptFile(commande);
    }

    [AllowAnonymous]
    public async Task<IActionResult> RecuPublic(Guid id, string token)
    {
        var commande = await db.CommandesBoutique
            .Include(c => c.Lignes).ThenInclude(l => l.ArticleBoutique)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.RecuToken == token);

        if (commande is null || commande.Statut is not (StatutCommandeBoutique.Payee or StatutCommandeBoutique.Livree))
        {
            return NotFound();
        }

        return BuildReceiptFile(commande);
    }

    private async Task<CommandeBoutique?> LoadCommandeAsync(Guid id, bool asNoTracking = false)
    {
        var query = db.CommandesBoutique
            .Include(c => c.Lignes).ThenInclude(l => l.ArticleBoutique)
            .Include(c => c.TraitePar)
            .Where(c => c.Id == id);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync();
    }

    private async Task MarkCommandeAsync(CommandeBoutique commande, StatutCommandeBoutique statut, string? commentaireTraitement)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) throw new InvalidOperationException("Utilisateur introuvable.");

        commande.Statut = statut;
        commande.DateTraitement = DateTime.UtcNow;
        commande.TraiteParId = user.Id;
        commande.CommentaireTraitement = NormalizeOptional(commentaireTraitement);
    }

    private FileContentResult BuildReceiptFile(CommandeBoutique commande)
    {
        var lines = new List<string>
        {
            $"Numero de recu : {commande.NumeroRecu}",
            $"Client : {commande.NomClient}",
            $"Telephone : {commande.TelephoneClient}",
            $"Email : {commande.EmailClient ?? "-"}",
            $"Mode de paiement : {FormatPaymentMode(commande.ModePaiement)}",
            $"Reference paiement : {commande.ReferencePaiement ?? "-"}",
            $"Date commande : {commande.DateCreation.ToLocalTime():dd/MM/yyyy HH:mm}",
            $"Statut : {commande.Statut}",
            string.Empty,
            "Articles :"
        };

        lines.AddRange(commande.Lignes.Select(l => $"- {l.ArticleBoutique.Nom} x {l.Quantite} : {(l.PrixUnitaire * l.Quantite):N0} {commande.Devise}"));
        lines.Add(string.Empty);
        lines.Add($"Total : {commande.Total:N0} {commande.Devise}");
        lines.Add("District Scout MANGO TAIKA");

        var bytes = SimplePdfBuilder.BuildTextPdf("Recu boutique - MANGO TAIKA", lines);
        return File(bytes, "application/pdf", $"{commande.NumeroRecu ?? "recu-boutique"}.pdf");
    }

    private async Task NotifyCustomerAsync(CommandeBoutique commande, string subject, string body, bool includeReceiptLink)
    {
        if (string.IsNullOrWhiteSpace(commande.EmailClient))
        {
            return;
        }

        var link = includeReceiptLink ? BuildPublicReceiptLink(commande) : null;
        await emailService.SendAsync(
            commande.EmailClient,
            subject,
            body,
            commande.NomClient,
            "Boutique",
            link);
    }

    private string BuildPublicReceiptLink(CommandeBoutique commande)
    {
        var baseUrl = configuration["App:PublicBaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = $"{Request.Scheme}://{Request.Host}";
        }

        return $"{baseUrl}/Boutique/RecuPublic/{commande.Id}?token={Uri.EscapeDataString(commande.RecuToken)}";
    }

    private void ValidateArticle(ArticleBoutique model)
    {
        if (string.IsNullOrWhiteSpace(model.Nom))
        {
            ModelState.AddModelError(nameof(model.Nom), "Le nom de l'article est obligatoire pour identifier le produit dans la boutique.");
        }

        if (model.Prix < 0)
        {
            ModelState.AddModelError(nameof(model.Prix), "Le prix ne peut pas etre negatif. Saisissez 0 pour un article gratuit ou un montant positif.");
        }

        if (model.StockDisponible < 0)
        {
            ModelState.AddModelError(nameof(model.StockDisponible), "Le stock ne peut pas etre negatif.");
        }

        model.Categorie = NormalizeOptional(model.Categorie);
    }

    private static string NormalizeCurrency(string? value)
        => string.IsNullOrWhiteSpace(value) ? "XOF" : value.Trim().ToUpperInvariant();

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string FormatPaymentMode(ModePaiementCommandeBoutique mode)
        => mode == ModePaiementCommandeBoutique.PaiementLivraison ? "Paiement a la livraison" : "Mobile Money";

    private static async Task<List<ArticleBoutique>> ReadArticlesFromCsvAsync(IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var lines = new List<string>();
        while (await reader.ReadLineAsync() is { } line)
        {
            if (!string.IsNullOrWhiteSpace(line)) lines.Add(line);
        }

        if (lines.Count < 2)
        {
            throw new InvalidOperationException("Le fichier CSV doit contenir une ligne d'entete et au moins un article.");
        }

        var delimiter = lines[0].Contains(';') ? ';' : ',';
        var headers = SplitDelimitedLine(lines[0], delimiter);
        return lines.Skip(1)
            .Select(line => BuildArticleFromRow(headers, SplitDelimitedLine(line, delimiter)))
            .Where(article => article is not null)
            .Select(article => article!)
            .ToList();
    }

    private static List<ArticleBoutique> ReadArticlesFromExcel(IFormFile file)
    {
        using var workbook = new XLWorkbook(file.OpenReadStream());
        var worksheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidOperationException("Le fichier Excel ne contient aucune feuille.");
        var rows = worksheet.RowsUsed().ToList();
        if (rows.Count < 2)
        {
            throw new InvalidOperationException("Le fichier Excel doit contenir une ligne d'entete et au moins un article.");
        }

        var headers = rows[0].CellsUsed().Select(c => c.GetString()).ToList();
        return rows.Skip(1)
            .Select(row => BuildArticleFromRow(headers, row.Cells(1, headers.Count).Select(c => c.GetString()).ToList()))
            .Where(article => article is not null)
            .Select(article => article!)
            .ToList();
    }

    private static ArticleBoutique? BuildArticleFromRow(IReadOnlyList<string> headers, IReadOnlyList<string> values)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Count; i++)
        {
            var header = NormalizeHeader(headers[i]);
            if (string.IsNullOrWhiteSpace(header)) continue;
            map[header] = i < values.Count ? values[i].Trim() : string.Empty;
        }

        var name = GetValue(map, "nom", "article", "libelle");
        if (string.IsNullOrWhiteSpace(name)) return null;

        var priceText = GetValue(map, "prix", "montant");
        if (!decimal.TryParse(priceText, NumberStyles.Number, CultureInfo.InvariantCulture, out var price)
            && !decimal.TryParse(priceText, NumberStyles.Number, CultureInfo.GetCultureInfo("fr-FR"), out price))
        {
            price = 0;
        }

        var stockText = GetValue(map, "stock", "quantite", "stockdisponible");
        _ = int.TryParse(stockText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var stock);

        var publishedText = GetValue(map, "publie", "estpublie", "statut");
        var published = string.IsNullOrWhiteSpace(publishedText)
            || publishedText.Equals("true", StringComparison.OrdinalIgnoreCase)
            || publishedText.Equals("oui", StringComparison.OrdinalIgnoreCase)
            || publishedText.Equals("publie", StringComparison.OrdinalIgnoreCase)
            || publishedText == "1";

        return new ArticleBoutique
        {
            Nom = name.Trim(),
            Categorie = NormalizeOptional(GetValue(map, "categorie", "catégorie", "category", "famille", "type")),
            Description = NormalizeOptional(GetValue(map, "description")),
            ImageUrl = NormalizeOptional(GetValue(map, "imageurl", "image", "urlimage")),
            Prix = price,
            Devise = NormalizeCurrency(GetValue(map, "devise", "currency")),
            StockDisponible = Math.Max(0, stock),
            EstPublie = published,
            DateCreation = DateTime.UtcNow
        };
    }

    private static List<string> SplitDelimitedLine(string line, char delimiter)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var character = line[i];
            if (character == '"' && i + 1 < line.Length && line[i + 1] == '"')
            {
                current.Append('"');
                i++;
                continue;
            }

            if (character == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (character == delimiter && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(character);
        }

        values.Add(current.ToString());
        return values;
    }

    private static string NormalizeHeader(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC)
            .Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty);
    }

    private static string? GetValue(Dictionary<string, string> map, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (map.TryGetValue(NormalizeHeader(key), out var value))
            {
                return value;
            }
        }

        return null;
    }

    private List<BoutiqueCartItem> GetCart()
    {
        var raw = HttpContext.Session.GetString(CartSessionKey);
        if (string.IsNullOrWhiteSpace(raw)) return [];

        try
        {
            return JsonSerializer.Deserialize<List<BoutiqueCartItem>>(raw) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private void SaveCart(List<BoutiqueCartItem> cart)
    {
        var normalizedCart = cart
            .Where(i => i.ArticleId != Guid.Empty && i.Quantite > 0)
            .GroupBy(i => i.ArticleId)
            .Select(g => new BoutiqueCartItem { ArticleId = g.Key, Quantite = g.Sum(i => i.Quantite) })
            .ToList();

        HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(normalizedCart));
    }

    private void ClearCart()
        => HttpContext.Session.Remove(CartSessionKey);

    private async Task<BoutiqueCartViewModel> BuildCartViewModelAsync()
    {
        var cart = GetCart();
        if (cart.Count == 0) return new BoutiqueCartViewModel();

        var articleIds = cart.Select(i => i.ArticleId).Distinct().ToList();
        var articles = await db.ArticlesBoutique
            .AsNoTracking()
            .Where(a => articleIds.Contains(a.Id) && a.EstPublie && !a.EstSupprime)
            .ToDictionaryAsync(a => a.Id);

        var mustResave = false;
        var lines = new List<BoutiqueCartLineViewModel>();
        foreach (var item in cart)
        {
            if (!articles.TryGetValue(item.ArticleId, out var article))
            {
                mustResave = true;
                continue;
            }

            var maxQuantity = article.StockDisponible > 0 ? article.StockDisponible : 99;
            var quantity = Math.Clamp(item.Quantite, 1, maxQuantity);
            if (quantity != item.Quantite)
            {
                item.Quantite = quantity;
                mustResave = true;
            }

            lines.Add(new BoutiqueCartLineViewModel
            {
                Article = article,
                Quantite = quantity,
                QuantiteMax = maxQuantity
            });
        }

        if (mustResave)
        {
            SaveCart(cart.Where(i => articles.ContainsKey(i.ArticleId)).ToList());
        }

        return new BoutiqueCartViewModel
        {
            Lignes = lines.OrderBy(l => l.Article.Nom).ToList()
        };
    }

    private IActionResult RedirectToSafeUrl(string? returnUrl, string fallbackAction, object? routeValues = null)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(fallbackAction, routeValues);
    }
}
