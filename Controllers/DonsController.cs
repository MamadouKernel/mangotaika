using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Helpers;
using MangoTaika.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

public class DonsController(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IEmailNotificationService emailService,
    IConfiguration configuration) : Controller
{
    [Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
    public async Task<IActionResult> Index(StatutDonPublic? statut)
    {
        var query = db.DonsPublics
            .Include(d => d.TraitePar)
            .AsNoTracking()
            .AsQueryable();

        if (statut.HasValue)
        {
            query = query.Where(d => d.Statut == statut.Value);
        }

        ViewBag.Statut = statut;
        return View(await query.OrderByDescending(d => d.DateCreation).ToListAsync());
    }

    [Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
    public async Task<IActionResult> Details(Guid id)
    {
        var don = await db.DonsPublics
            .Include(d => d.TraitePar)
            .Include(d => d.TransactionFinanciere)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);

        return don is null ? NotFound() : View(don);
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirmer(Guid id, string? commentaireTraitement)
    {
        var don = await db.DonsPublics.FirstOrDefaultAsync(d => d.Id == id);
        if (don is null) return NotFound();

        if (don.Statut == StatutDonPublic.Confirme)
        {
            TempData["Info"] = "Ce don est deja confirme.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        don.Statut = StatutDonPublic.Confirme;
        don.DateTraitement = DateTime.UtcNow;
        don.TraiteParId = user.Id;
        don.CommentaireTraitement = NormalizeOptional(commentaireTraitement);
        don.NumeroRecu ??= $"DON-{DateTime.UtcNow:yyyyMMdd}-{don.Id.ToString("N")[..8].ToUpperInvariant()}";

        var transaction = new TransactionFinanciere
        {
            Id = Guid.NewGuid(),
            Libelle = $"Don - {don.NomDonateur}",
            Montant = don.Montant,
            Type = TypeTransaction.Recette,
            Categorie = CategorieFinance.Don,
            DateTransaction = DateTime.UtcNow,
            Reference = don.ReferencePaiement ?? don.NumeroRecu,
            Commentaire = don.Message,
            CreateurId = user.Id
        };

        db.TransactionsFinancieres.Add(transaction);
        don.TransactionFinanciereId = transaction.Id;
        await db.SaveChangesAsync();

        await NotifyDonorAsync(don, "Don confirme", BuildConfirmedMessage(don), includeReceiptLink: true);
        TempData["Success"] = "Don confirme, recette finance creee et email envoye si une adresse est renseignee.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rejeter(Guid id, string? commentaireTraitement)
    {
        var don = await db.DonsPublics.FirstOrDefaultAsync(d => d.Id == id);
        if (don is null) return NotFound();

        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        don.Statut = StatutDonPublic.Rejete;
        don.DateTraitement = DateTime.UtcNow;
        don.TraiteParId = user.Id;
        don.CommentaireTraitement = NormalizeOptional(commentaireTraitement);
        await db.SaveChangesAsync();

        await NotifyDonorAsync(don, "Don non confirme", BuildRejectedMessage(don), includeReceiptLink: false);
        TempData["Success"] = "Don rejete et email envoye si une adresse est renseignee.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
    public async Task<IActionResult> Recu(Guid id)
    {
        var don = await db.DonsPublics.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        if (don is null) return NotFound();
        if (don.Statut != StatutDonPublic.Confirme)
        {
            TempData["Error"] = "Le recu est disponible uniquement apres confirmation du don.";
            return RedirectToAction(nameof(Details), new { id });
        }

        return BuildReceiptFile(don);
    }

    [AllowAnonymous]
    public async Task<IActionResult> RecuPublic(Guid id, string token)
    {
        var don = await db.DonsPublics.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id && d.RecuToken == token);
        if (don is null || don.Statut != StatutDonPublic.Confirme)
        {
            return NotFound();
        }

        return BuildReceiptFile(don);
    }

    private FileContentResult BuildReceiptFile(DonPublic don)
    {
        var bytes = SimplePdfBuilder.BuildTextPdf(
            "Recu de don - MANGO TAIKA",
            [
                $"Numero de recu : {don.NumeroRecu}",
                $"Donateur : {don.NomDonateur}",
                $"Montant : {don.Montant:N0} {don.Devise}",
                $"Reference paiement : {don.ReferencePaiement ?? "-"}",
                $"Date de declaration : {don.DateCreation.ToLocalTime():dd/MM/yyyy HH:mm}",
                $"Date de confirmation : {don.DateTraitement?.ToLocalTime():dd/MM/yyyy HH:mm}",
                "Statut : Confirme",
                string.Empty,
                "District Scout MANGO TAIKA",
                "Merci pour votre contribution au developpement des activites scoutes."
            ]);

        return File(bytes, "application/pdf", $"{don.NumeroRecu ?? "recu-don"}.pdf");
    }

    private async Task NotifyDonorAsync(DonPublic don, string subject, string body, bool includeReceiptLink)
    {
        if (string.IsNullOrWhiteSpace(don.Email))
        {
            return;
        }

        var link = includeReceiptLink ? BuildPublicReceiptLink(don) : null;
        await emailService.SendAsync(
            don.Email,
            subject,
            body,
            don.NomDonateur,
            "Don",
            link);
    }

    private string BuildPublicReceiptLink(DonPublic don)
    {
        var baseUrl = configuration["App:PublicBaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = $"{Request.Scheme}://{Request.Host}";
        }

        return $"{baseUrl}/Dons/RecuPublic/{don.Id}?token={Uri.EscapeDataString(don.RecuToken)}";
    }

    private static string BuildConfirmedMessage(DonPublic don)
        => $"""
        Votre don a ete confirme par le District Scout MANGO TAIKA.
        Montant : {don.Montant:N0} {don.Devise}
        Reference : {don.ReferencePaiement ?? don.NumeroRecu ?? "-"}
        Statut : Confirme
        Merci pour votre soutien.
        """;

    private static string BuildRejectedMessage(DonPublic don)
        => $"""
        Votre declaration de don n'a pas pu etre confirmee pour le moment.
        Montant declare : {don.Montant:N0} {don.Devise}
        Reference : {don.ReferencePaiement ?? "-"}
        Statut : Non confirme
        {don.CommentaireTraitement ?? "Merci de verifier la reference du paiement ou de contacter le district."}
        """;

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
