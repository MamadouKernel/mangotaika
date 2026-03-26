using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace MangoTaika.Helpers;

/// <summary>
/// Pagination des listes admin : 6 par défaut, choix 6 / 10 / 20 / 50 / 100.
/// </summary>
public static class ListPagination
{
    public static readonly int[] AllowedPageSizes = [6, 10, 20, 50, 100];
    public const int DefaultPageSize = 6;

    /// <summary>Clé pour HttpContext.Items : le ViewData n'est pas toujours propagé aux partials.</summary>
    public static readonly object PaginationItemsKey = new();

    public sealed record PaginationState(int Page, int PageSize, int Total, int TotalPages);

    public static (int Page, int PageSize) Read(HttpRequest request)
    {
        int.TryParse(request.Query["page"].FirstOrDefault(), out var page);
        int.TryParse(request.Query["pageSize"].FirstOrDefault(), out var pageSize);
        page = page < 1 ? 1 : page;
        pageSize = AllowedPageSizes.Contains(pageSize) ? pageSize : DefaultPageSize;
        return (page, pageSize);
    }

    /// <summary>Recalcule page si hors plage ; retourne skip et nombre de pages.</summary>
    public static (int Page, int PageSize, int Skip, int TotalPages) Normalize(int page, int pageSize, int totalCount)
    {
        pageSize = AllowedPageSizes.Contains(pageSize) ? pageSize : DefaultPageSize;
        page = page < 1 ? 1 : page;
        var totalPages = totalCount == 0 ? 1 : Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
        if (page > totalPages) page = totalPages;
        var skip = (page - 1) * pageSize;
        return (page, pageSize, skip, totalPages);
    }

    public static void SetViewData(ViewDataDictionary vd, HttpContext http, int page, int pageSize, int totalCount, int totalPages)
    {
        vd["PaginationPage"] = page;
        vd["PaginationPageSize"] = pageSize;
        vd["PaginationTotal"] = totalCount;
        vd["PaginationTotalPages"] = totalPages;
        vd["PaginationAllowedSizes"] = AllowedPageSizes;
        http.Items[PaginationItemsKey] = new PaginationState(page, pageSize, totalCount, totalPages);
    }

    /// <summary>Lit un entier depuis ViewData (les int boxés ne passent pas avec « as int? » en Razor).</summary>
    public static int ReadViewDataInt(ViewDataDictionary vd, string key, int fallback)
    {
        var v = vd[key];
        return v switch
        {
            int i => i,
            uint u => (int)u,
            long l => (int)l,
            short s => s,
            byte b => b,
            _ => fallback
        };
    }

    /// <summary>Conserve les paramètres GET sauf page / pageSize.</summary>
    public static string PagingQuery(HttpRequest request, int page, int pageSize)
    {
        var qb = new QueryBuilder();
        foreach (var kv in request.Query)
        {
            if (string.Equals(kv.Key, "page", StringComparison.OrdinalIgnoreCase)) continue;
            if (string.Equals(kv.Key, "pageSize", StringComparison.OrdinalIgnoreCase)) continue;
            foreach (var v in kv.Value)
            {
                if (!string.IsNullOrEmpty(v))
                    qb.Add(kv.Key, v);
            }
        }
        qb.Add("page", page.ToString());
        qb.Add("pageSize", pageSize.ToString());
        return qb.ToQueryString().Value ?? "";
    }
}
