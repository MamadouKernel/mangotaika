using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
using MangoTaika.Helpers;
using MangoTaika.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

[Authorize]
public class ForumFormationsController(
    IFormationService formationService,
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    INotificationDispatchService notificationDispatchService) : Controller
{
    [Authorize(Roles = "Administrateur,Gestionnaire,Scout,Superviseur,Consultant")]
    public async Task<IActionResult> Index(Guid formationId)
    {
        var access = await ResolveFormationAccessAsync(formationId);
        if (access.Result is not null)
            return access.Result;

        var discussions = await formationService.GetDiscussionsAsync(formationId);
        return View(new ForumFormationPageDto
        {
            FormationId = access.FormationId,
            FormationTitre = access.FormationTitre,
            LectureSeule = access.LectureSeule,
            PeutParticiper = access.PeutParticiper,
            PeutModerer = access.PeutModerer,
            Discussions = discussions
        });
    }

    [Authorize(Roles = "Administrateur,Gestionnaire,Scout,Superviseur,Consultant")]
    public async Task<IActionResult> Discussion(Guid id)
    {
        var discussion = await formationService.GetDiscussionAsync(id);
        if (discussion is null)
            return NotFound();

        var access = await ResolveFormationAccessAsync(discussion.FormationId);
        if (access.Result is not null)
            return access.Result;

        return View(new DiscussionFormationPageDto
        {
            FormationId = discussion.FormationId,
            FormationTitre = discussion.FormationTitre,
            LectureSeule = access.LectureSeule,
            PeutParticiper = access.PeutParticiper,
            PeutModerer = access.PeutModerer,
            Discussion = discussion
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire,Scout")]
    public async Task<IActionResult> NouvelleDiscussion(Guid formationId, DiscussionFormationCreateDto dto)
    {
        var access = await ResolveFormationAccessAsync(formationId);
        if (access.Result is not null)
            return access.Result;

        if (!access.PeutParticiper)
            return Forbid();

        if (string.IsNullOrWhiteSpace(dto.Titre) || string.IsNullOrWhiteSpace(dto.Contenu))
        {
            TempData["Error"] = "Le titre et le contenu de la discussion sont obligatoires.";
            return RedirectToAction(nameof(Index), new { formationId });
        }

        var user = await userManager.GetUserAsync(User);
        var discussion = await formationService.AjouterDiscussionAsync(formationId, user!.Id, dto);
        var recipients = await GetFormationRecipientsAsync(formationId, user.Id);
        if (recipients.Count != 0)
        {
            await notificationDispatchService.SendAsync(
                recipients,
                "Nouvelle discussion de cours",
                $"Un nouveau sujet a ete publie dans \"{access.FormationTitre}\": {discussion.Titre}",
                "LMS",
                $"/ForumFormations/Discussion/{discussion.Id}");
        }
        TempData["Success"] = "Discussion creee avec succes.";
        return RedirectToAction(nameof(Discussion), new { id = discussion.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire,Scout")]
    public async Task<IActionResult> AjouterMessage(Guid discussionId, MessageDiscussionFormationCreateDto dto)
    {
        var formationId = await db.DiscussionsFormation
            .AsNoTracking()
            .Where(d => d.Id == discussionId)
            .Select(d => (Guid?)d.FormationId)
            .FirstOrDefaultAsync();

        if (!formationId.HasValue)
            return NotFound();

        var access = await ResolveFormationAccessAsync(formationId.Value);
        if (access.Result is not null)
            return access.Result;

        if (!access.PeutParticiper)
            return Forbid();

        if (string.IsNullOrWhiteSpace(dto.Contenu))
        {
            TempData["Error"] = "La reponse ne peut pas etre vide.";
            return RedirectToAction(nameof(Discussion), new { id = discussionId });
        }

        try
        {
            var user = await userManager.GetUserAsync(User);
            await formationService.AjouterMessageDiscussionAsync(discussionId, user!.Id, dto);
            var recipients = await GetDiscussionRecipientsAsync(discussionId, user.Id);
            if (recipients.Count != 0)
            {
                var discussion = await formationService.GetDiscussionAsync(discussionId);
                if (discussion != null)
                {
                    await notificationDispatchService.SendAsync(
                        recipients,
                        "Nouvelle reponse dans le forum",
                        $"Nouvelle reponse dans \"{discussion.Titre}\" pour la formation \"{discussion.FormationTitre}\".",
                        "LMS",
                        $"/ForumFormations/Discussion/{discussionId}");
                }
            }
            TempData["Success"] = "Reponse ajoutee.";
        }
        catch (InvalidOperationException ex)
        {
            this.SetDomainError(ex);
        }

        return RedirectToAction(nameof(Discussion), new { id = discussionId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> BasculerVerrou(Guid discussionId)
    {
        var discussion = await formationService.GetDiscussionAsync(discussionId);
        if (discussion is null)
            return NotFound();

        await formationService.BasculerVerrouDiscussionAsync(discussionId);
        TempData["Success"] = discussion.EstVerrouillee
            ? "La discussion a ete deverrouillee."
            : "La discussion a ete verrouillee.";
        return RedirectToAction(nameof(Discussion), new { id = discussionId });
    }

    private async Task<ForumAccessResolution> ResolveFormationAccessAsync(Guid formationId)
    {
        var formation = await db.Formations
            .AsNoTracking()
            .Where(f => f.Id == formationId)
            .Select(f => new { f.Id, f.Titre })
            .FirstOrDefaultAsync();

        if (formation is null)
        {
            return new ForumAccessResolution
            {
                Result = NotFound()
            };
        }

        var canModerate = User.IsInRole("Administrateur") || User.IsInRole("Gestionnaire");
        if (canModerate)
        {
            return new ForumAccessResolution
            {
                FormationId = formation.Id,
                FormationTitre = formation.Titre,
                PeutParticiper = true,
                PeutModerer = true
            };
        }

        if (User.IsInRole("Superviseur") || User.IsInRole("Consultant"))
        {
            return new ForumAccessResolution
            {
                FormationId = formation.Id,
                FormationTitre = formation.Titre,
                LectureSeule = true
            };
        }

        if (User.IsInRole("Scout"))
        {
            var scout = await GetCurrentScoutAsync();
            if (scout is null || !await formationService.EstInscritAsync(formationId, scout.Id))
            {
                return new ForumAccessResolution
                {
                    Result = Forbid()
                };
            }

            return new ForumAccessResolution
            {
                FormationId = formation.Id,
                FormationTitre = formation.Titre,
                PeutParticiper = true
            };
        }

        return new ForumAccessResolution
        {
            Result = Forbid()
        };
    }

    private async Task<Scout?> GetCurrentScoutAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return null;

        return await db.Scouts.FirstOrDefaultAsync(s => s.UserId == user.Id && s.IsActive);
    }

    private async Task<List<Guid>> GetFormationRecipientsAsync(Guid formationId, Guid actorUserId)
    {
        var enrolledUsers = await db.InscriptionsFormation
            .AsNoTracking()
            .Where(i => i.FormationId == formationId && i.Scout.UserId != null)
            .Select(i => i.Scout.UserId!.Value)
            .Distinct()
            .ToListAsync();

        var teamUsers = await db.UserRoles
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .Where(x => x.Name == "Administrateur" || x.Name == "Gestionnaire")
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync();

        return enrolledUsers
            .Concat(teamUsers)
            .Where(id => id != actorUserId)
            .Distinct()
            .ToList();
    }

    private async Task<List<Guid>> GetDiscussionRecipientsAsync(Guid discussionId, Guid actorUserId)
    {
        var discussionAuthorId = await db.DiscussionsFormation
            .AsNoTracking()
            .Where(d => d.Id == discussionId)
            .Select(d => (Guid?)d.AuteurId)
            .FirstOrDefaultAsync();

        if (!discussionAuthorId.HasValue)
            return [];

        var participantIds = await db.MessagesDiscussionFormation
            .AsNoTracking()
            .Where(m => m.DiscussionFormationId == discussionId && !m.EstSupprime)
            .Select(m => m.AuteurId)
            .Distinct()
            .ToListAsync();

        return participantIds
            .Append(discussionAuthorId.Value)
            .Where(id => id != actorUserId)
            .Distinct()
            .ToList();
    }

    private sealed class ForumAccessResolution
    {
        public Guid FormationId { get; set; }
        public string FormationTitre { get; set; } = string.Empty;
        public bool LectureSeule { get; set; }
        public bool PeutParticiper { get; set; }
        public bool PeutModerer { get; set; }
        public IActionResult? Result { get; set; }
    }
}
