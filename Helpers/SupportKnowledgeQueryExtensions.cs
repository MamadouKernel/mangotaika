using MangoTaika.Data;
using MangoTaika.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Helpers;

public static class SupportKnowledgeQueryExtensions
{
    public static IQueryable<SupportKnowledgeArticle> ApplyTextSearch(
        this IQueryable<SupportKnowledgeArticle> query,
        AppDbContext db,
        string term)
    {
        var pattern = DatabaseText.ToNormalizedContainsPattern(term);
        return db.Database.IsNpgsql()
            ? query.Where(a =>
                EF.Functions.Like(PostgresTextFunctions.NormalizeSearch(a.Titre), pattern) ||
                EF.Functions.Like(PostgresTextFunctions.NormalizeSearch(a.Resume), pattern) ||
                EF.Functions.Like(PostgresTextFunctions.NormalizeSearch(a.Contenu), pattern) ||
                (a.MotsCles != null && EF.Functions.Like(PostgresTextFunctions.NormalizeSearch(a.MotsCles), pattern)))
            : query;
    }

    public static IQueryable<SupportKnowledgeArticle> ApplyServiceSuggestion(
        this IQueryable<SupportKnowledgeArticle> query,
        AppDbContext db,
        string category,
        string serviceName,
        string serviceCode)
    {
        if (!db.Database.IsNpgsql())
        {
            return query;
        }

        var categoryPattern = DatabaseText.ToNormalizedContainsPattern(category);
        var serviceNamePattern = DatabaseText.ToNormalizedContainsPattern(serviceName);
        var serviceCodePattern = DatabaseText.ToNormalizedContainsPattern(serviceCode);

        return query.Where(a =>
            EF.Functions.Like(PostgresTextFunctions.NormalizeSearch(a.Categorie), categoryPattern) ||
            EF.Functions.Like(PostgresTextFunctions.NormalizeSearch(a.Titre), serviceNamePattern) ||
            EF.Functions.Like(PostgresTextFunctions.NormalizeSearch(a.Resume), serviceNamePattern) ||
            (a.MotsCles != null &&
                (EF.Functions.Like(PostgresTextFunctions.NormalizeSearch(a.MotsCles), serviceNamePattern) ||
                 EF.Functions.Like(PostgresTextFunctions.NormalizeSearch(a.MotsCles), serviceCodePattern))));
    }
}
