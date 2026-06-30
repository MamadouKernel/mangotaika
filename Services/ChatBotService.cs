using MangoTaika.Data;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Services;

public class ChatBotService(AppDbContext db) : IChatBotService
{
    public async Task<List<ChatBotItemDto>> GetMesTicketsAsync(Guid userId)
    {
        return await db.Tickets
            .Where(t => t.CreateurId == userId)
            .OrderByDescending(t => t.DateCreation)
            .Take(10)
            .Select(t => new ChatBotItemDto(t.Id, $"{t.NumeroTicket} - {t.Sujet}", t.Statut.ToString(), t.DateCreation))
            .ToListAsync();
    }

    public async Task<List<ChatBotItemDto>> GetMesDemandesAsync(Guid userId)
    {
        return await db.DemandesAutorisation
            .Where(d => d.DemandeurId == userId)
            .OrderByDescending(d => d.DateCreation)
            .Take(10)
            .Select(d => new ChatBotItemDto(d.Id, d.Titre, d.Statut.ToString(), d.DateCreation))
            .ToListAsync();
    }

    public async Task<List<ChatBotItemDto>> GetMesActivitesAsync(Guid userId)
    {
        return await db.Activites
            .Where(a => a.CreateurId == userId)
            .OrderByDescending(a => a.DateCreation)
            .Take(10)
            .Select(a => new ChatBotItemDto(a.Id, a.Titre, a.Statut.ToString(), a.DateCreation))
            .ToListAsync();
    }

    public async Task<List<(Guid Id, string Question, string Reponse)>> RechercherFaqAsync(string motsCles)
    {
        if (string.IsNullOrWhiteSpace(motsCles))
        {
            return await db.FaqEntries
                .Where(f => f.EstActif)
                .OrderBy(f => f.OrdreAffichage)
                .Take(10)
                .Select(f => new ValueTuple<Guid, string, string>(f.Id, f.Question, f.Reponse))
                .ToListAsync();
        }

        var terme = motsCles.Trim().ToLowerInvariant();
        return await db.FaqEntries
            .Where(f => f.EstActif && (
                f.Question.ToLower().Contains(terme) ||
                (f.MotsCles != null && f.MotsCles.ToLower().Contains(terme))))
            .OrderBy(f => f.OrdreAffichage)
            .Take(10)
            .Select(f => new ValueTuple<Guid, string, string>(f.Id, f.Question, f.Reponse))
            .ToListAsync();
    }

    public async Task<List<ChatBotArticleDto>> GetArticlesBoutiqueAsync(string? recherche = null)
    {
        var query = db.ArticlesBoutique.Where(a => a.EstPublie);

        if (!string.IsNullOrWhiteSpace(recherche))
        {
            var terme = recherche.Trim().ToLowerInvariant();
            query = query.Where(a => a.Nom.ToLower().Contains(terme)
                || (a.Categorie != null && a.Categorie.ToLower().Contains(terme))
                || (a.Description != null && a.Description.ToLower().Contains(terme)));
        }

        return await query
            .OrderBy(a => a.Categorie).ThenBy(a => a.Nom)
            .Take(20)
            .Select(a => new ChatBotArticleDto(a.Id, a.Nom, a.Categorie, a.Prix, a.Devise, a.StockDisponible > 0))
            .ToListAsync();
    }

    public async Task<List<ChatBotItemDto>> GetActivitesPubliquesAsync()
    {
        return await db.Activites
            .Where(a => a.Statut == Data.Entities.StatutActivite.Validee || a.Statut == Data.Entities.StatutActivite.EnCours)
            .OrderBy(a => a.DateDebut)
            .Take(15)
            .Select(a => new ChatBotItemDto(a.Id, a.Titre, a.Lieu ?? "Lieu non précisé", a.DateDebut))
            .ToListAsync();
    }

    public async Task<List<ChatBotActualiteDto>> GetActualitesRecentesAsync()
    {
        return await db.Actualites
            .Where(a => a.EstPublie)
            .OrderByDescending(a => a.DatePublication)
            .Take(10)
            .Select(a => new ChatBotActualiteDto(a.Id, a.Titre, a.Resume, a.DatePublication))
            .ToListAsync();
    }
}
