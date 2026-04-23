using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
using MangoTaika.Helpers;
using MangoTaika.Hubs;
using MangoTaika.Services;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

[Authorize]
public class TicketsController(
    ITicketService ticketService,
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    IHubContext<NotificationHub> hubContext,
    IFileUploadService fileUploadService,
    IWebHostEnvironment env) : Controller
{
    [Authorize(Roles = "Administrateur,Gestionnaire,AgentSupport,Superviseur,Consultant")]
    public async Task<IActionResult> Index(
        StatutTicket? statut,
        TypeTicket? type,
        CategorieTicket? categorie,
        PrioriteTicket? priorite,
        string? vue,
        string? recherche,
        string? tri)
    {
        var (page, ps) = ListPagination.Read(Request);
        var currentUserId = Guid.Parse(userManager.GetUserId(User)!);
        var allTickets = await ticketService.GetAllAsync(statut, type, categorie, priorite, vue, recherche, currentUserId);
        var total = allTickets.Count;
        var (p, pageSize, skip, totalPages) = ListPagination.Normalize(page, ps, total);
        var tickets = allTickets.Skip(skip).Take(pageSize).ToList();

        ViewBag.Dashboard = await ticketService.GetSupportDashboardAsync(currentUserId);
        ViewBag.FiltreStatut = statut;
        ViewBag.FiltreType = type;
        ViewBag.FiltreCategorie = categorie;
        ViewBag.FiltrePriorite = priorite;
        ViewBag.Recherche = recherche;
        ViewBag.Vue = vue ?? "all";
        ViewBag.TicketsTotal = total;
        ViewBag.Tri = tri ?? "sla";

        ViewBag.Agents = await GetSupportAgentsAsync();

        ListPagination.SetViewData(ViewData, HttpContext, p, pageSize, total, totalPages);
        return View(tickets);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire,AgentSupport,Superviseur,Consultant")]
    public async Task<IActionResult> ExportExcel(
        StatutTicket? statut,
        TypeTicket? type,
        CategorieTicket? categorie,
        PrioriteTicket? priorite,
        string? vue,
        string? recherche)
    {
        var currentUserId = Guid.Parse(userManager.GetUserId(User)!);
        var tickets = await ticketService.GetAllAsync(statut, type, categorie, priorite, vue, recherche, currentUserId);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Support");
        var headers = new[]
        {
            "Numero", "Sujet", "Type", "Categorie", "Service", "Priorite", "Statut",
            "Demandeur", "Agent", "Groupe", "DateCreation", "DateLimiteSla", "EtatSla"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#313e45");
            ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
        }

        for (var row = 0; row < tickets.Count; row++)
        {
            var ticket = tickets[row];
            var excelRow = row + 2;
            ws.Cell(excelRow, 1).Value = ticket.NumeroTicket;
            ws.Cell(excelRow, 2).Value = ticket.Sujet;
            ws.Cell(excelRow, 3).Value = ticket.Type.ToString();
            ws.Cell(excelRow, 4).Value = ticket.Categorie.ToString();
            ws.Cell(excelRow, 5).Value = ticket.NomServiceCatalogue ?? "";
            ws.Cell(excelRow, 6).Value = ticket.Priorite.ToString();
            ws.Cell(excelRow, 7).Value = ticket.Statut.ToString();
            ws.Cell(excelRow, 8).Value = ticket.NomCreateur ?? "";
            ws.Cell(excelRow, 9).Value = ticket.NomAssigne ?? "";
            ws.Cell(excelRow, 10).Value = ticket.NomGroupeAssigne ?? "";
            ws.Cell(excelRow, 11).Value = ticket.DateCreation;
            ws.Cell(excelRow, 12).Value = ticket.DateLimiteSla;
            ws.Cell(excelRow, 13).Value = ticket.EstEnRetard ? "Depasse" : $"OK ({Math.Abs(ticket.HeuresAvantSla):N0} h)";
        }

        ws.Columns().AdjustToContents();
        ws.Column(11).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
        ws.Column(12).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"support-{DateTime.UtcNow:yyyyMMdd-HHmm}.xlsx");
    }

    [Authorize(Roles = "Administrateur,Gestionnaire,AgentSupport,Superviseur,Consultant")]
    public async Task<IActionResult> Print(
        StatutTicket? statut,
        TypeTicket? type,
        CategorieTicket? categorie,
        PrioriteTicket? priorite,
        string? vue,
        string? recherche)
    {
        var currentUserId = Guid.Parse(userManager.GetUserId(User)!);
        var tickets = await ticketService.GetAllAsync(statut, type, categorie, priorite, vue, recherche, currentUserId);
        ViewBag.Recherche = recherche;
        ViewBag.Vue = vue ?? "all";
        return View("Print", tickets);
    }

    public async Task<IActionResult> MesTickets()
    {
        var userId = Guid.Parse(userManager.GetUserId(User)!);
        var tickets = await ticketService.GetByUserAsync(userId);
        return View(tickets);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire,AgentSupport,Scout,Parent")]
    public async Task<IActionResult> Create()
    {
        ViewBag.ServiceCatalogItems = await db.SupportCatalogueServices
            .Where(s => s.EstActif)
            .OrderBy(s => s.Nom)
            .ToListAsync();
        return View(new TicketCreateDto());
    }

    [HttpGet]
    [Authorize(Roles = "Administrateur,Gestionnaire,AgentSupport,Scout,Parent")]
    public async Task<IActionResult> SuggestKnowledge(string? q, Guid? serviceId)
    {
        var query = db.SupportKnowledgeArticles
            .Where(a => a.EstPublie)
            .AsQueryable();

        if (!db.Database.IsNpgsql())
        {
            var articles = await query.ToListAsync();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var normalizedTerm = DatabaseText.NormalizeSearchKey(q);
                articles = articles
                    .Where(a =>
                        DatabaseText.ContainsNormalized(a.Titre, normalizedTerm) ||
                        DatabaseText.ContainsNormalized(a.Resume, normalizedTerm) ||
                        DatabaseText.ContainsNormalized(a.Contenu, normalizedTerm) ||
                        DatabaseText.ContainsNormalized(a.MotsCles, normalizedTerm))
                    .ToList();
            }

            if (serviceId.HasValue)
            {
                var service = await db.SupportCatalogueServices
                    .Where(s => s.Id == serviceId.Value)
                    .Select(s => new { s.Nom, s.Code, Category = s.CategorieParDefaut.ToString() })
                    .FirstOrDefaultAsync();

                if (service is not null)
                {
                    var normalizedCategory = DatabaseText.NormalizeSearchKey(service.Category);
                    var normalizedServiceName = DatabaseText.NormalizeSearchKey(service.Nom);
                    var normalizedServiceCode = DatabaseText.NormalizeSearchKey(service.Code);

                    articles = articles
                        .Where(a =>
                            DatabaseText.ContainsNormalized(a.Categorie, normalizedCategory) ||
                            DatabaseText.ContainsNormalized(a.Titre, normalizedServiceName) ||
                            DatabaseText.ContainsNormalized(a.Resume, normalizedServiceName) ||
                            DatabaseText.ContainsNormalized(a.MotsCles, normalizedServiceName) ||
                            DatabaseText.ContainsNormalized(a.MotsCles, normalizedServiceCode))
                        .ToList();
                }
            }

            var fallbackItems = articles
                .OrderByDescending(a => a.DateMiseAJour ?? a.DateCreation)
                .Take(5)
                .Select(a => new
                {
                    id = a.Id,
                    titre = a.Titre,
                    resume = a.Resume,
                    contenu = a.Contenu.Length > 600 ? a.Contenu.Substring(0, 600) + "..." : a.Contenu,
                    categorie = a.Categorie,
                    url = Url.Action("Details", "KnowledgeBase", new { id = a.Id })
                })
                .ToList();

            return Json(fallbackItems);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.ApplyTextSearch(db, q);
        }

        if (serviceId.HasValue)
        {
            var service = await db.SupportCatalogueServices
                .Where(s => s.Id == serviceId.Value)
                .Select(s => new { s.Nom, s.Code, Category = s.CategorieParDefaut.ToString() })
                .FirstOrDefaultAsync();

            if (service is not null)
            {
                query = query.ApplyServiceSuggestion(db, service.Category, service.Nom, service.Code);
            }
        }

        var items = await query
            .OrderByDescending(a => a.DateMiseAJour ?? a.DateCreation)
            .Take(5)
            .Select(a => new
            {
                id = a.Id,
                titre = a.Titre,
                resume = a.Resume,
                contenu = a.Contenu.Length > 600 ? a.Contenu.Substring(0, 600) + "..." : a.Contenu,
                categorie = a.Categorie,
                url = Url.Action("Details", "KnowledgeBase", new { id = a.Id })
            })
            .ToListAsync();

        return Json(items);
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire,AgentSupport,Scout,Parent")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TicketCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ServiceCatalogItems = await db.SupportCatalogueServices
                .Where(s => s.EstActif)
                .OrderBy(s => s.Nom)
                .ToListAsync();
            return View(dto);
        }

        var userId = Guid.Parse(userManager.GetUserId(User)!);
        await ticketService.CreateAsync(dto, userId);
        TempData["Success"] = "Ticket cree avec succes. Il a ete place dans la file de support.";

        await hubContext.Clients.All.SendAsync("RecevoirNotification", $"Nouveau ticket : {dto.Sujet}");

        if (User.IsInRole("Administrateur") || User.IsInRole("Gestionnaire") || User.IsInRole("AgentSupport"))
        {
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction(nameof(MesTickets));
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var ticket = await ticketService.GetByIdAsync(id);
        if (ticket is null)
        {
            return NotFound();
        }

        var userId = Guid.Parse(userManager.GetUserId(User)!);
        var canViewAll = User.IsInRole("Administrateur") || User.IsInRole("Gestionnaire") || User.IsInRole("AgentSupport") || User.IsInRole("Superviseur") || User.IsInRole("Consultant");
        var canManageTicket = User.IsInRole("Administrateur") || User.IsInRole("Gestionnaire") || User.IsInRole("AgentSupport");

        if (!canViewAll && ticket.CreateurId != userId)
        {
            return Forbid();
        }

        if (!canManageTicket)
        {
            ticket.Messages = ticket.Messages.Where(m => !m.EstNoteInterne).ToList();
        }

        if (canManageTicket)
        {
            ViewBag.Agents = await GetSupportAgentsAsync();
            ViewBag.Groupes = await db.Groupes.Where(g => g.IsActive).OrderBy(g => g.Nom).ToListAsync();
        }

        ViewBag.IsAdmin = canViewAll;
        ViewBag.CanManageTicket = canManageTicket;
        ViewBag.CurrentUserId = userId;
        return View(ticket);
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire,AgentSupport,Scout,Parent")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AjouterMessage(Guid ticketId, string contenu)
    {
        if (string.IsNullOrWhiteSpace(contenu))
        {
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        var userId = Guid.Parse(userManager.GetUserId(User)!);
        var ticket = await db.Tickets.Include(t => t.Createur).FirstOrDefaultAsync(t => t.Id == ticketId);
        if (ticket is null)
        {
            return NotFound();
        }
        if (!(User.IsInRole("Administrateur") || User.IsInRole("Gestionnaire") || User.IsInRole("AgentSupport")) && ticket.CreateurId != userId)
        {
            return Forbid();
        }

        await ticketService.AjouterMessageAsync(ticketId, contenu, userId);

        var isSupport = User.IsInRole("Administrateur") || User.IsInRole("Gestionnaire") || User.IsInRole("AgentSupport");
        var targetId = isSupport ? ticket.CreateurId.ToString() : (ticket.AssigneAId?.ToString() ?? "");
        if (!string.IsNullOrEmpty(targetId))
        {
            await hubContext.Clients.User(targetId).SendAsync("RecevoirNotification", $"Nouveau message sur le ticket : {ticket.Sujet}");
        }

        TempData["Success"] = "Message envoye.";
        return RedirectToAction(nameof(Details), new { id = ticketId });
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire,AgentSupport")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AjouterNoteInterne(Guid ticketId, string contenu)
    {
        if (string.IsNullOrWhiteSpace(contenu))
        {
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        var userId = Guid.Parse(userManager.GetUserId(User)!);
        await ticketService.AjouterNoteInterneAsync(ticketId, contenu, userId);
        TempData["Success"] = "Note interne ajoutee.";
        return RedirectToAction(nameof(Details), new { id = ticketId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire,AgentSupport")]
    public async Task<IActionResult> Assigner(Guid ticketId, Guid agentId)
    {
        await ticketService.AssignerAsync(ticketId, agentId);
        var agent = await userManager.FindByIdAsync(agentId.ToString());
        TempData["Success"] = $"Ticket assigne a {agent?.Prenom} {agent?.Nom}.";
        return RedirectToAction(nameof(Details), new { id = ticketId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire,AgentSupport")]
    public async Task<IActionResult> ChangerStatut(Guid ticketId, StatutTicket statut)
    {
        var userId = Guid.Parse(userManager.GetUserId(User)!);
        await ticketService.UpdateStatutAsync(ticketId, statut, userId);
        TempData["Success"] = $"Statut mis a jour : {statut}.";
        return RedirectToAction(nameof(Details), new { id = ticketId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire,AgentSupport")]
    public async Task<IActionResult> AssignerGroupe(Guid ticketId, Guid groupeId)
    {
        await ticketService.AssignerGroupeAsync(ticketId, groupeId);
        var groupe = await db.Groupes.FindAsync(groupeId);
        TempData["Success"] = $"Ticket transfere au groupe {groupe?.Nom}.";
        return RedirectToAction(nameof(Details), new { id = ticketId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire,AgentSupport")]
    public async Task<IActionResult> Recategoriser(Guid ticketId, TypeTicket type, CategorieTicket categorie, ImpactTicket impact, UrgenceTicket urgence)
    {
        await ticketService.RecategoriserAsync(ticketId, type, categorie, impact, urgence);
        TempData["Success"] = "Ticket recategorise avec succes.";
        return RedirectToAction(nameof(Details), new { id = ticketId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire,AgentSupport")]
    public async Task<IActionResult> Resoudre(TicketResolutionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ResumeResolution))
        {
            TempData["ImportError"] = "Le resume de resolution est obligatoire.";
            return RedirectToAction(nameof(Details), new { id = dto.TicketId });
        }

        var userId = Guid.Parse(userManager.GetUserId(User)!);
        await ticketService.ResoudreAsync(dto.TicketId, dto.ResumeResolution, dto.FermerApresResolution, userId);
        TempData["Success"] = dto.FermerApresResolution ? "Ticket resolu et ferme." : "Ticket resolu.";
        return RedirectToAction(nameof(Details), new { id = dto.TicketId });
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire,Scout,Parent")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Noter(Guid ticketId, int note, string? commentaire)
    {
        var ticket = await ticketService.GetByIdAsync(ticketId);
        if (ticket is null)
        {
            return NotFound();
        }

        var userId = Guid.Parse(userManager.GetUserId(User)!);
        if (ticket.CreateurId != userId)
        {
            return Forbid();
        }

        await ticketService.NoterAsync(ticketId, note, commentaire);
        TempData["Success"] = "Merci pour votre evaluation.";
        return RedirectToAction(nameof(Details), new { id = ticketId });
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire,AgentSupport,Scout,Parent")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AjouterPieceJointe(Guid ticketId, IFormFile? fichier)
    {
        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId && !t.EstSupprime);
        if (ticket is null)
        {
            return NotFound();
        }

        var userId = Guid.Parse(userManager.GetUserId(User)!);
        var canManageTicket = User.IsInRole("Administrateur") || User.IsInRole("Gestionnaire") || User.IsInRole("AgentSupport");
        if (!canManageTicket && ticket.CreateurId != userId)
        {
            return Forbid();
        }

        if (fichier is null || fichier.Length == 0)
        {
            TempData["ImportError"] = "Aucun fichier selectionne.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        if (fichier.Length > 15 * 1024 * 1024)
        {
            TempData["ImportError"] = "La taille maximale autorisee est de 15 Mo.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        try
        {
            var url = await fileUploadService.SaveFileAsync(fichier, "tickets");
            db.TicketPiecesJointes.Add(new TicketPieceJointe
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                AjouteParId = userId,
                NomOriginal = Path.GetFileName(fichier.FileName),
                UrlFichier = url,
                TypeMime = fichier.ContentType,
                TailleOctets = fichier.Length
            });
            await db.SaveChangesAsync();
            TempData["Success"] = "Piece jointe ajoutee.";
        }
        catch (InvalidOperationException ex)
        {
            this.SetDomainError(ex);
        }

        return RedirectToAction(nameof(Details), new { id = ticketId });
    }

    [Authorize(Roles = "Administrateur,Gestionnaire,AgentSupport,Superviseur,Consultant,Scout,Parent")]
    public async Task<IActionResult> TelechargerPieceJointe(Guid id)
    {
        var piece = await db.TicketPiecesJointes
            .Include(p => p.Ticket)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (piece is null)
        {
            return NotFound();
        }

        var userId = Guid.Parse(userManager.GetUserId(User)!);
        var canViewAll = User.IsInRole("Administrateur") || User.IsInRole("Gestionnaire") || User.IsInRole("AgentSupport") || User.IsInRole("Superviseur") || User.IsInRole("Consultant");
        if (!canViewAll && piece.Ticket.CreateurId != userId)
        {
            return Forbid();
        }

        var relativePath = piece.UrlFichier.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(env.WebRootPath, relativePath);
        if (!System.IO.File.Exists(fullPath))
        {
            return NotFound();
        }

        return PhysicalFile(fullPath, piece.TypeMime ?? "application/octet-stream", piece.NomOriginal);
    }

    private async Task<List<ApplicationUser>> GetSupportAgentsAsync()
    {
        var admins = await userManager.GetUsersInRoleAsync("Administrateur");
        var commissairesDistrict = await userManager.GetUsersInRoleAsync("CommissaireDistrict");
        var gestionnaires = await userManager.GetUsersInRoleAsync("Gestionnaire");
        var agents = await userManager.GetUsersInRoleAsync("AgentSupport");
        return admins.Concat(commissairesDistrict).Concat(gestionnaires).Concat(agents).Distinct().OrderBy(u => u.Prenom).ThenBy(u => u.Nom).ToList();
    }
}
