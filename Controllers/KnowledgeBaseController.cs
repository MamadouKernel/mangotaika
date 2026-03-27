using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

[Authorize(Roles = "Administrateur,Gestionnaire,AgentSupport,Superviseur,Consultant")]
public class KnowledgeBaseController(
    AppDbContext db,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index(string? recherche, string? categorie)
    {
        var query = db.SupportKnowledgeArticles
            .Include(a => a.Auteur)
            .AsQueryable();

        if (!CanManage())
        {
            query = query.Where(a => a.EstPublie);
        }

        if (!string.IsNullOrWhiteSpace(recherche))
        {
            query = query.ApplyTextSearch(db, recherche);
        }

        if (!string.IsNullOrWhiteSpace(categorie))
        {
            query = query.Where(a => a.Categorie == categorie);
        }

        List<SupportKnowledgeArticle> articles;
        if (db.Database.IsNpgsql())
        {
            articles = await query
                .OrderByDescending(a => a.EstPublie)
                .ThenByDescending(a => a.DateMiseAJour ?? a.DateCreation)
                .ToListAsync();
        }
        else
        {
            articles = await query.ToListAsync();

            if (!string.IsNullOrWhiteSpace(recherche))
            {
                var normalizedTerm = DatabaseText.NormalizeSearchKey(recherche);
                articles = articles
                    .Where(a =>
                        DatabaseText.ContainsNormalized(a.Titre, normalizedTerm) ||
                        DatabaseText.ContainsNormalized(a.Resume, normalizedTerm) ||
                        DatabaseText.ContainsNormalized(a.Contenu, normalizedTerm) ||
                        DatabaseText.ContainsNormalized(a.MotsCles, normalizedTerm))
                    .ToList();
            }

            articles = articles
                .OrderByDescending(a => a.EstPublie)
                .ThenByDescending(a => a.DateMiseAJour ?? a.DateCreation)
                .ToList();
        }

        ViewBag.CanManage = CanManage();
        ViewBag.Categories = await db.SupportKnowledgeArticles
            .Select(a => a.Categorie)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
        ViewBag.Recherche = recherche;
        ViewBag.Categorie = categorie;
        return View(articles);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var article = await db.SupportKnowledgeArticles
            .Include(a => a.Auteur)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (article is null || (!article.EstPublie && !CanManage()))
        {
            return NotFound();
        }

        ViewBag.CanManage = CanManage();
        return View(article);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire,AgentSupport")]
    public IActionResult Create() => View(new SupportKnowledgeArticle { Categorie = "General" });

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire,AgentSupport")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupportKnowledgeArticle model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        model.Id = Guid.NewGuid();
        model.AuteurId = Guid.Parse(userManager.GetUserId(User)!);
        model.DateCreation = DateTime.UtcNow;
        model.DateMiseAJour = DateTime.UtcNow;
        db.SupportKnowledgeArticles.Add(model);
        await db.SaveChangesAsync();
        TempData["Success"] = "Article cree.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrateur,Gestionnaire,AgentSupport")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var article = await db.SupportKnowledgeArticles.FindAsync(id);
        return article is null ? NotFound() : View(article);
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire,AgentSupport")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, SupportKnowledgeArticle model)
    {
        var article = await db.SupportKnowledgeArticles.FindAsync(id);
        if (article is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        article.Titre = model.Titre.Trim();
        article.Resume = model.Resume.Trim();
        article.Contenu = model.Contenu.Trim();
        article.Categorie = model.Categorie.Trim();
        article.MotsCles = model.MotsCles?.Trim();
        article.EstPublie = model.EstPublie;
        article.DateMiseAJour = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["Success"] = "Article mis a jour.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire,AgentSupport")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(Guid id)
    {
        var article = await db.SupportKnowledgeArticles.FindAsync(id);
        if (article is null)
        {
            return NotFound();
        }

        article.EstPublie = !article.EstPublie;
        article.DateMiseAJour = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["Success"] = article.EstPublie ? "Article publie." : "Article passe en brouillon.";
        return RedirectToAction(nameof(Index));
    }

    private bool CanManage() =>
        User.IsInRole("Administrateur") || User.IsInRole("Gestionnaire") || User.IsInRole("AgentSupport");
}
