using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
using MangoTaika.Helpers;
using MangoTaika.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Globalization;
using System.Text;

namespace MangoTaika.Controllers;

[Authorize]
public class PortefeuillesController(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IEmailNotificationService emailService,
    IConfiguration configuration) : Controller
{
    private Guid UserId => Guid.Parse(userManager.GetUserId(User)!);

    public async Task<IActionResult> MonPortefeuille()
    {
        var portefeuille = await EnsureCurrentUserWalletAsync();
        await LoadWalletActionDataAsync();
        var limits = await BuildTransferLimitsAsync(portefeuille);
        ViewBag.TransferMinimum = limits.Minimum;
        ViewBag.TransferMaximum = limits.Maximum;
        ViewBag.TransferDailyRemaining = limits.DailyRemaining;
        return View(portefeuille);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DonnerDepuisPortefeuille(decimal montant, string? commentaire)
    {
        if (montant <= 0)
        {
            TempData["Error"] = "Le montant du don doit etre superieur a 0.";
            return RedirectToAction(nameof(MonPortefeuille));
        }

        var portefeuille = await EnsureCurrentUserWalletAsync();
        if (portefeuille.Solde < montant)
        {
            TempData["Error"] = "Solde insuffisant pour effectuer ce don.";
            return RedirectToAction(nameof(MonPortefeuille));
        }

        var user = await userManager.GetUserAsync(User);
        var reference = $"DON-WAL-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        var before = portefeuille.Solde;
        portefeuille.Solde -= montant;

        var mouvement = new MouvementPortefeuille
        {
            Id = Guid.NewGuid(),
            PortefeuilleUtilisateurId = portefeuille.Id,
            PortefeuilleUtilisateur = portefeuille,
            Type = TypeMouvementPortefeuille.Don,
            Statut = StatutMouvementPortefeuille.Valide,
            Montant = montant,
            Devise = portefeuille.Devise,
            Libelle = "Don au district depuis le portefeuille",
            Reference = reference,
            Commentaire = NormalizeOptional(commentaire),
            RecuToken = Guid.NewGuid().ToString("N"),
            NumeroRecu = reference,
            SoldeAvant = before,
            SoldeApres = portefeuille.Solde,
            DateValidation = DateTime.UtcNow,
            ValideParId = user?.Id
        };

        var transaction = new TransactionFinanciere
        {
            Id = Guid.NewGuid(),
            Libelle = mouvement.Libelle,
            Montant = montant,
            Type = TypeTransaction.Recette,
            Categorie = CategorieFinance.Don,
            DateTransaction = DateTime.UtcNow,
            Reference = reference,
            Commentaire = mouvement.Commentaire,
            CreateurId = user?.Id ?? UserId
        };

        db.MouvementsPortefeuilles.Add(mouvement);
        db.TransactionsFinancieres.Add(transaction);
        mouvement.TransactionFinanciereId = transaction.Id;
        await db.SaveChangesAsync();

        await NotifyWalletOwnerAsync(mouvement, "Don portefeuille confirme", BuildWalletMessage(mouvement, "Valide"), includeReceiptLink: true);
        TempData["Success"] = "Don effectue depuis votre portefeuille.";
        return RedirectToAction(nameof(MonPortefeuille));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayerDepuisPortefeuille(string typePaiement, decimal montant, Guid? activiteId, Guid? comptePaiementId, string? commentaire)
    {
        if (montant <= 0)
        {
            TempData["Error"] = "Paiement impossible : saisissez un montant superieur a 0.";
            return RedirectToAction(nameof(MonPortefeuille));
        }

        var portefeuille = await EnsureCurrentUserWalletAsync();
        if (portefeuille.Solde < montant)
        {
            TempData["Error"] = $"Solde insuffisant : votre portefeuille contient {portefeuille.Solde:N0} {portefeuille.Devise}, le paiement demande est de {montant:N0} {portefeuille.Devise}. Rechargez votre portefeuille ou reduisez le montant.";
            return RedirectToAction(nameof(MonPortefeuille));
        }

        var isCotisation = string.Equals(typePaiement, "Cotisation", StringComparison.OrdinalIgnoreCase);
        var isActivite = string.Equals(typePaiement, "Activite", StringComparison.OrdinalIgnoreCase);
        if (!isCotisation && !isActivite)
        {
            TempData["Error"] = "Paiement impossible : selectionnez Cotisation nationale ou Activite.";
            return RedirectToAction(nameof(MonPortefeuille));
        }

        Activite? activite = null;
        if (isActivite)
        {
            if (!activiteId.HasValue)
            {
                TempData["Error"] = "Paiement d'activite impossible : selectionnez l'activite concernee.";
                return RedirectToAction(nameof(MonPortefeuille));
            }

            activite = await db.Activites.FirstOrDefaultAsync(a => a.Id == activiteId.Value && !a.EstSupprime);
            if (activite is null)
            {
                TempData["Error"] = "Paiement impossible : l'activite selectionnee est introuvable ou a ete supprimee. Actualisez la page puis reessayez.";
                return RedirectToAction(nameof(MonPortefeuille));
            }
        }

        var compte = comptePaiementId.HasValue
            ? await db.ComptesPaiementMobile.FirstOrDefaultAsync(c => c.Id == comptePaiementId.Value && c.EstActif && !c.EstSupprime)
            : isActivite
                ? await db.ComptesPaiementMobile
                    .Where(c => c.EstActif && !c.EstSupprime && c.ActiviteId == activite!.Id)
                    .OrderByDescending(c => c.EstPrincipal)
                    .FirstOrDefaultAsync()
                    ?? await db.ComptesPaiementMobile.Where(c => c.EstActif && !c.EstSupprime).OrderByDescending(c => c.EstPrincipal).FirstOrDefaultAsync()
                : await db.ComptesPaiementMobile.Where(c => c.EstActif && !c.EstSupprime).OrderByDescending(c => c.EstPrincipal).FirstOrDefaultAsync();

        if (comptePaiementId.HasValue && compte is null)
        {
            TempData["Error"] = "Compte de paiement introuvable ou inactif. Choisissez un autre compte ou demandez a l'administration de l'activer.";
            return RedirectToAction(nameof(MonPortefeuille));
        }

        if (isActivite && compte?.ActiviteId is Guid linkedActivityId && linkedActivityId != activite!.Id)
        {
            TempData["Error"] = "Le compte de paiement selectionne est rattache a une autre activite. Utilisez le compte de cette activite ou un compte global.";
            return RedirectToAction(nameof(MonPortefeuille));
        }

        var user = await userManager.GetUserAsync(User);
        var reference = $"PAY-WAL-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        var before = portefeuille.Solde;
        portefeuille.Solde -= montant;

        var libelle = isCotisation
            ? "Paiement cotisation nationale depuis le portefeuille"
            : $"Paiement activite - {activite!.Titre}";

        var mouvement = new MouvementPortefeuille
        {
            Id = Guid.NewGuid(),
            PortefeuilleUtilisateurId = portefeuille.Id,
            PortefeuilleUtilisateur = portefeuille,
            Type = TypeMouvementPortefeuille.Paiement,
            Statut = StatutMouvementPortefeuille.Valide,
            Montant = montant,
            Devise = portefeuille.Devise,
            Libelle = libelle,
            Reference = reference,
            Commentaire = BuildPaymentComment(compte, commentaire),
            RecuToken = Guid.NewGuid().ToString("N"),
            NumeroRecu = reference,
            SoldeAvant = before,
            SoldeApres = portefeuille.Solde,
            DateValidation = DateTime.UtcNow,
            ValideParId = user?.Id
        };

        var transaction = new TransactionFinanciere
        {
            Id = Guid.NewGuid(),
            Libelle = libelle,
            Montant = montant,
            Type = TypeTransaction.Recette,
            Categorie = isCotisation ? CategorieFinance.Cotisation : CategorieFinance.Activite,
            DateTransaction = DateTime.UtcNow,
            Reference = reference,
            Commentaire = mouvement.Commentaire,
            CreateurId = user?.Id ?? UserId,
            ActiviteId = activite?.Id,
            GroupeId = activite?.GroupeId
        };

        db.MouvementsPortefeuilles.Add(mouvement);
        db.TransactionsFinancieres.Add(transaction);
        mouvement.TransactionFinanciereId = transaction.Id;
        await db.SaveChangesAsync();

        await NotifyWalletOwnerAsync(mouvement, "Paiement portefeuille confirme", BuildWalletMessage(mouvement, "Valide"), includeReceiptLink: true);
        TempData["Success"] = "Paiement effectue depuis votre portefeuille.";
        return RedirectToAction(nameof(MonPortefeuille));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DemanderRecharge(decimal montant, string? reference, string? commentaire)
    {
        if (montant <= 0)
        {
            TempData["Error"] = "Le montant de recharge doit etre superieur a 0.";
            return RedirectToAction(nameof(MonPortefeuille));
        }

        var portefeuille = await EnsureCurrentUserWalletAsync();
        var mouvement = new MouvementPortefeuille
        {
            Id = Guid.NewGuid(),
            PortefeuilleUtilisateurId = portefeuille.Id,
            Type = TypeMouvementPortefeuille.Rechargement,
            Statut = StatutMouvementPortefeuille.EnAttente,
            Montant = montant,
            Devise = portefeuille.Devise,
            Libelle = "Demande de recharge portefeuille",
            Reference = NormalizeOptional(reference),
            Commentaire = NormalizeOptional(commentaire),
            RecuToken = Guid.NewGuid().ToString("N")
        };

        db.MouvementsPortefeuilles.Add(mouvement);
        await db.SaveChangesAsync();
        TempData["Success"] = "Demande de recharge enregistree. Elle sera validee apres verification du paiement.";
        return RedirectToAction(nameof(MonPortefeuille));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Transferer(string beneficiaire, decimal montant, string? commentaire)
    {
        var senderWallet = await EnsureCurrentUserWalletAsync();
        await LoadWalletActionDataAsync();

        var limitError = await ValidateTransferLimitsAsync(senderWallet, montant);
        if (limitError is not null)
        {
            TempData["Error"] = limitError;
            return RedirectToAction(nameof(MonPortefeuille));
        }

        var candidates = await FindBeneficiaryCandidatesAsync(beneficiaire);
        if (candidates.Count == 0)
        {
            TempData["Error"] = "Beneficiaire introuvable. Recherchez avec un telephone, un email, un matricule ou le nom complet exactement comme sur son compte.";
            return RedirectToAction(nameof(MonPortefeuille));
        }

        var limits = await BuildTransferLimitsAsync(senderWallet);
        var model = new ConfirmTransferViewModel
        {
            BeneficiaireId = candidates.Count == 1 ? candidates[0].Id : Guid.Empty,
            Montant = montant,
            Commentaire = NormalizeOptional(commentaire),
            SoldeActuel = senderWallet.Solde,
            Devise = senderWallet.Devise,
            MontantMinimum = limits.Minimum,
            MontantMaximum = limits.Maximum,
            PlafondJournalierRestant = limits.DailyRemaining,
            Candidats = candidates
        };

        return View("ConfirmerTransfert", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExecuterTransfert(ConfirmTransferViewModel model)
    {
        var senderWallet = await EnsureCurrentUserWalletAsync();
        var beneficiaryUser = await db.Users.FirstOrDefaultAsync(u => u.Id == model.BeneficiaireId && u.IsActive);
        if (beneficiaryUser is null || beneficiaryUser.Id == UserId)
        {
            TempData["Error"] = "Transfert impossible : le beneficiaire est inactif, introuvable ou correspond a votre propre compte.";
            return RedirectToAction(nameof(MonPortefeuille));
        }

        var limitError = await ValidateTransferLimitsAsync(senderWallet, model.Montant);
        if (limitError is not null)
        {
            TempData["Error"] = limitError;
            return RedirectToAction(nameof(MonPortefeuille));
        }

        var beneficiaryWallet = await EnsureWalletAsync(beneficiaryUser.Id);
        var transfertId = Guid.NewGuid();
        var reference = $"TRF-{DateTime.UtcNow:yyyyMMdd}-{transfertId.ToString("N")[..8].ToUpperInvariant()}";
        var now = DateTime.UtcNow;
        var commentaire = NormalizeOptional(model.Commentaire);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        await db.Entry(senderWallet).ReloadAsync();
        await db.Entry(beneficiaryWallet).ReloadAsync();

        if (senderWallet.Solde < model.Montant)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = "Solde insuffisant pour effectuer ce transfert.";
            return RedirectToAction(nameof(MonPortefeuille));
        }

        var senderBefore = senderWallet.Solde;
        var beneficiaryBefore = beneficiaryWallet.Solde;

        senderWallet.Solde -= model.Montant;
        beneficiaryWallet.Solde += model.Montant;

        var debit = new MouvementPortefeuille
        {
            Id = Guid.NewGuid(),
            PortefeuilleUtilisateurId = senderWallet.Id,
            PortefeuilleUtilisateur = senderWallet,
            Type = TypeMouvementPortefeuille.Debit,
            Statut = StatutMouvementPortefeuille.Valide,
            Montant = model.Montant,
            Devise = senderWallet.Devise,
            Libelle = $"Transfert vers {beneficiaryUser.Prenom} {beneficiaryUser.Nom}".Trim(),
            Reference = reference,
            TransfertId = transfertId,
            RecuToken = Guid.NewGuid().ToString("N"),
            NumeroRecu = $"{reference}-D",
            Commentaire = commentaire,
            SoldeAvant = senderBefore,
            SoldeApres = senderWallet.Solde,
            AdresseIp = ip,
            UserAgent = userAgent,
            DateCreation = now,
            DateValidation = now,
            ValideParId = UserId
        };

        var credit = new MouvementPortefeuille
        {
            Id = Guid.NewGuid(),
            PortefeuilleUtilisateurId = beneficiaryWallet.Id,
            PortefeuilleUtilisateur = beneficiaryWallet,
            Type = TypeMouvementPortefeuille.Credit,
            Statut = StatutMouvementPortefeuille.Valide,
            Montant = model.Montant,
            Devise = beneficiaryWallet.Devise,
            Libelle = $"Transfert recu de {senderWallet.User.Prenom} {senderWallet.User.Nom}".Trim(),
            Reference = reference,
            TransfertId = transfertId,
            RecuToken = Guid.NewGuid().ToString("N"),
            NumeroRecu = $"{reference}-C",
            Commentaire = commentaire,
            SoldeAvant = beneficiaryBefore,
            SoldeApres = beneficiaryWallet.Solde,
            AdresseIp = ip,
            UserAgent = userAgent,
            DateCreation = now,
            DateValidation = now,
            ValideParId = UserId
        };

        db.MouvementsPortefeuilles.AddRange(debit, credit);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        await NotifyWalletOwnerAsync(debit, "Transfert portefeuille envoye", BuildWalletMessage(debit, "Valide"), includeReceiptLink: true);
        await NotifyWalletOwnerAsync(credit, "Transfert portefeuille recu", BuildWalletMessage(credit, "Valide"), includeReceiptLink: true);

        TempData["Success"] = "Transfert effectue automatiquement.";
        return RedirectToAction(nameof(MonPortefeuille));
    }

    [Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
    public async Task<IActionResult> Index(StatutMouvementPortefeuille? statut)
    {
        var query = db.MouvementsPortefeuilles
            .Include(m => m.PortefeuilleUtilisateur).ThenInclude(p => p.User)
            .Include(m => m.ValidePar)
            .AsNoTracking()
            .AsQueryable();

        if (statut.HasValue)
        {
            query = query.Where(m => m.Statut == statut.Value);
        }

        ViewBag.Statut = statut;
        return View(await query.OrderByDescending(m => m.DateCreation).ToListAsync());
    }

    [Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
    public async Task<IActionResult> Details(Guid id)
    {
        var mouvement = await LoadMovementAsync(id, asNoTracking: true);
        return mouvement is null ? NotFound() : View(mouvement);
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Valider(Guid id, string? commentaire)
    {
        var mouvement = await LoadMovementAsync(id);
        if (mouvement is null) return NotFound();

        if (mouvement.Statut == StatutMouvementPortefeuille.Valide)
        {
            TempData["Info"] = "Ce mouvement est deja valide.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (mouvement.Statut != StatutMouvementPortefeuille.EnAttente)
        {
            TempData["Error"] = "Seul un mouvement en attente peut etre valide.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        try
        {
            ApplyMovementToBalance(mouvement);
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
        mouvement.Statut = StatutMouvementPortefeuille.Valide;
        mouvement.DateValidation = DateTime.UtcNow;
        mouvement.ValideParId = user.Id;
        mouvement.Commentaire = NormalizeOptional(commentaire) ?? mouvement.Commentaire;
        mouvement.NumeroRecu ??= $"WAL-{DateTime.UtcNow:yyyyMMdd}-{mouvement.Id.ToString("N")[..8].ToUpperInvariant()}";

        if (mouvement.Type is TypeMouvementPortefeuille.Don)
        {
            var transaction = new TransactionFinanciere
            {
                Id = Guid.NewGuid(),
                Libelle = mouvement.Libelle,
                Montant = mouvement.Montant,
                Type = TypeTransaction.Recette,
                Categorie = CategorieFinance.Autre,
                DateTransaction = DateTime.UtcNow,
                Reference = mouvement.Reference ?? mouvement.NumeroRecu,
                Commentaire = mouvement.Commentaire,
                CreateurId = user.Id
            };
            db.TransactionsFinancieres.Add(transaction);
            mouvement.TransactionFinanciereId = transaction.Id;
        }

        await db.SaveChangesAsync();
        await NotifyWalletOwnerAsync(mouvement, "Mouvement portefeuille valide", BuildWalletMessage(mouvement, "Valide"), includeReceiptLink: true);
        TempData["Success"] = "Mouvement valide et solde mis a jour.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rejeter(Guid id, string? commentaire)
    {
        var mouvement = await LoadMovementAsync(id);
        if (mouvement is null) return NotFound();

        if (mouvement.Statut != StatutMouvementPortefeuille.EnAttente)
        {
            TempData["Error"] = "Seul un mouvement en attente peut etre rejete.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        mouvement.Statut = StatutMouvementPortefeuille.Rejete;
        mouvement.DateValidation = DateTime.UtcNow;
        mouvement.ValideParId = user.Id;
        mouvement.Commentaire = NormalizeOptional(commentaire) ?? mouvement.Commentaire;
        await db.SaveChangesAsync();
        await NotifyWalletOwnerAsync(mouvement, "Mouvement portefeuille rejete", BuildWalletMessage(mouvement, "Rejete"), includeReceiptLink: false);
        TempData["Success"] = "Mouvement rejete.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreerMouvement(Guid userId, TypeMouvementPortefeuille type, decimal montant, string libelle, string? reference, string? commentaire)
    {
        if (montant <= 0 || string.IsNullOrWhiteSpace(libelle))
        {
            TempData["Error"] = "Creation impossible : renseignez un libelle clair et un montant superieur a 0.";
            return RedirectToAction(nameof(Index));
        }

        var portefeuille = await EnsureWalletAsync(userId);
        var mouvement = new MouvementPortefeuille
        {
            Id = Guid.NewGuid(),
            PortefeuilleUtilisateurId = portefeuille.Id,
            Type = type,
            Statut = StatutMouvementPortefeuille.EnAttente,
            Montant = montant,
            Devise = portefeuille.Devise,
            Libelle = libelle.Trim(),
            Reference = NormalizeOptional(reference),
            Commentaire = NormalizeOptional(commentaire),
            RecuToken = Guid.NewGuid().ToString("N")
        };

        db.MouvementsPortefeuilles.Add(mouvement);
        await db.SaveChangesAsync();
        TempData["Success"] = "Mouvement cree en attente de validation.";
        return RedirectToAction(nameof(Details), new { id = mouvement.Id });
    }

    [Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
    public async Task<IActionResult> Recu(Guid id)
    {
        var mouvement = await LoadMovementAsync(id, asNoTracking: true);
        if (mouvement is null) return NotFound();
        if (mouvement.Statut != StatutMouvementPortefeuille.Valide)
        {
            TempData["Error"] = "Le recu n'est pas encore disponible : le mouvement doit d'abord etre valide.";
            return RedirectToAction(nameof(Details), new { id });
        }

        return BuildReceiptFile(mouvement);
    }

    [AllowAnonymous]
    public async Task<IActionResult> RecuPublic(Guid id, string token)
    {
        var mouvement = await db.MouvementsPortefeuilles
            .Include(m => m.PortefeuilleUtilisateur).ThenInclude(p => p.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id && m.RecuToken == token && m.Statut == StatutMouvementPortefeuille.Valide);

        return mouvement is null ? NotFound() : BuildReceiptFile(mouvement);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
    public async Task<IActionResult> ComptesPaiement()
    {
        ViewBag.Activites = await db.Activites
            .Where(a => !a.EstSupprime && (a.Statut == StatutActivite.Validee || a.Statut == StatutActivite.EnCours))
            .OrderByDescending(a => a.DateDebut)
            .Take(100)
            .ToListAsync();
        return View(await db.ComptesPaiementMobile
            .Include(c => c.Activite)
            .Where(c => !c.EstSupprime)
            .OrderByDescending(c => c.EstPrincipal)
            .ThenBy(c => c.Libelle)
            .ToListAsync());
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnregistrerComptePaiement(Guid? id, string libelle, string operateur, string numeroMobile, string? nomTitulaire, Guid? activiteId)
    {
        if (string.IsNullOrWhiteSpace(libelle) || string.IsNullOrWhiteSpace(operateur) || string.IsNullOrWhiteSpace(numeroMobile))
        {
            TempData["Error"] = "Compte de paiement incomplet : renseignez le libelle, l'operateur et le numero mobile.";
            return RedirectToAction(nameof(ComptesPaiement));
        }

        var estPrincipal = Request.Form["estPrincipal"].Any(v => string.Equals(v, "true", StringComparison.OrdinalIgnoreCase));
        var estActif = Request.Form["estActif"].Any(v => string.Equals(v, "true", StringComparison.OrdinalIgnoreCase));
        if (!estActif)
        {
            estPrincipal = false;
        }

        var user = await userManager.GetUserAsync(User);

        if (estPrincipal)
        {
            var anciensPrincipaux = await db.ComptesPaiementMobile
                .Where(c => !c.EstSupprime && c.EstPrincipal && (!id.HasValue || c.Id != id.Value))
                .ToListAsync();
            foreach (var compte in anciensPrincipaux)
            {
                compte.EstPrincipal = false;
            }
        }

        if (id.HasValue)
        {
            var compte = await db.ComptesPaiementMobile.FirstOrDefaultAsync(c => c.Id == id.Value && !c.EstSupprime);
            if (compte is null) return NotFound();

            compte.Libelle = libelle.Trim();
            compte.Operateur = operateur.Trim();
            compte.NumeroMobile = numeroMobile.Trim();
            compte.NomTitulaire = NormalizeOptional(nomTitulaire);
            compte.ActiviteId = activiteId == Guid.Empty ? null : activiteId;
            compte.EstPrincipal = estPrincipal;
            compte.EstActif = estActif;
            compte.ModifieParId = user?.Id;
        }
        else
        {
            db.ComptesPaiementMobile.Add(new ComptePaiementMobile
            {
                Id = Guid.NewGuid(),
                Libelle = libelle.Trim(),
                Operateur = operateur.Trim(),
                NumeroMobile = numeroMobile.Trim(),
                NomTitulaire = NormalizeOptional(nomTitulaire),
                ActiviteId = activiteId == Guid.Empty ? null : activiteId,
                EstPrincipal = estPrincipal,
                EstActif = estActif,
                ModifieParId = user?.Id
            });
        }

        await db.SaveChangesAsync();
        TempData["Success"] = id.HasValue ? "Compte de paiement mobile mis a jour." : "Compte de paiement mobile enregistre.";
        return RedirectToAction(nameof(ComptesPaiement));
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire,CommissaireDistrict")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SupprimerComptePaiement(Guid id)
    {
        var compte = await db.ComptesPaiementMobile.FirstOrDefaultAsync(c => c.Id == id && !c.EstSupprime);
        if (compte is null)
        {
            return NotFound();
        }

        var user = await userManager.GetUserAsync(User);
        compte.EstSupprime = true;
        compte.EstActif = false;
        compte.EstPrincipal = false;
        compte.ModifieParId = user?.Id;

        await db.SaveChangesAsync();
        TempData["Success"] = "Compte de paiement mobile supprime de la liste.";
        return RedirectToAction(nameof(ComptesPaiement));
    }

    private async Task<PortefeuilleUtilisateur> EnsureCurrentUserWalletAsync()
        => await EnsureWalletAsync(UserId);

    private async Task<PortefeuilleUtilisateur> EnsureWalletAsync(Guid userId)
    {
        var portefeuille = await db.PortefeuillesUtilisateurs
            .Include(p => p.User)
            .Include(p => p.Mouvements.OrderByDescending(m => m.DateCreation))
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (portefeuille is not null)
        {
            return portefeuille;
        }

        var owner = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (owner is null)
        {
            throw new InvalidOperationException("Utilisateur introuvable pour la creation du portefeuille.");
        }

        portefeuille = new PortefeuilleUtilisateur
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            User = owner
        };
        db.PortefeuillesUtilisateurs.Add(portefeuille);
        await db.SaveChangesAsync();
        return portefeuille;
    }

    private async Task<List<TransferCandidateDto>> FindBeneficiaryCandidatesAsync(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var normalizedQuery = NormalizeSearch(query);
        var rawQuery = query.Trim();

        var users = await db.Users
            .Where(u => u.IsActive && u.Id != UserId)
            .ToListAsync();

        return users
            .Where(u =>
                NormalizeSearch(u.PhoneNumber) == normalizedQuery ||
                NormalizeSearch(u.Email) == normalizedQuery ||
                NormalizeSearch(u.Matricule) == normalizedQuery ||
                NormalizeSearch($"{u.Prenom} {u.Nom}").Contains(normalizedQuery) ||
                NormalizeSearch($"{u.Nom} {u.Prenom}").Contains(normalizedQuery) ||
                string.Equals(u.UserName, rawQuery, StringComparison.OrdinalIgnoreCase))
            .OrderBy(u => NormalizeSearch(u.Matricule) == normalizedQuery ? 0 : 1)
            .ThenBy(u => NormalizeSearch(u.PhoneNumber) == normalizedQuery ? 0 : 1)
            .ThenBy(u => u.Nom)
            .ThenBy(u => u.Prenom)
            .Take(10)
            .Select(MapTransferCandidate)
            .ToList();
    }

    private async Task<(decimal Minimum, decimal Maximum, decimal DailyRemaining)> BuildTransferLimitsAsync(PortefeuilleUtilisateur wallet)
    {
        var minimum = GetConfiguredAmount("Wallet:Transfer:Minimum", 100);
        var maximum = GetConfiguredAmount("Wallet:Transfer:Maximum", 500000);
        var dailyLimit = GetConfiguredAmount("Wallet:Transfer:DailyLimit", 1000000);
        var startOfDay = DateTime.UtcNow.Date;
        var dailyUsed = await db.MouvementsPortefeuilles
            .Where(m => m.PortefeuilleUtilisateurId == wallet.Id
                && m.Type == TypeMouvementPortefeuille.Debit
                && m.Statut == StatutMouvementPortefeuille.Valide
                && m.DateCreation >= startOfDay)
            .SumAsync(m => (decimal?)m.Montant) ?? 0;

        return (minimum, maximum, Math.Max(0, dailyLimit - dailyUsed));
    }

    private async Task<string?> ValidateTransferLimitsAsync(PortefeuilleUtilisateur wallet, decimal amount)
    {
        var limits = await BuildTransferLimitsAsync(wallet);
        if (amount <= 0)
        {
            return "Le montant du transfert doit etre superieur a 0.";
        }
        if (amount < limits.Minimum)
        {
            return $"Le montant minimum de transfert est {limits.Minimum:N0} {wallet.Devise}.";
        }
        if (amount > limits.Maximum)
        {
            return $"Le montant maximum par transfert est {limits.Maximum:N0} {wallet.Devise}.";
        }
        if (amount > limits.DailyRemaining)
        {
            return $"Plafond journalier insuffisant. Restant disponible : {limits.DailyRemaining:N0} {wallet.Devise}.";
        }
        if (wallet.Solde < amount)
        {
            return "Solde insuffisant pour effectuer ce transfert.";
        }

        return null;
    }

    private decimal GetConfiguredAmount(string key, decimal fallback)
        => decimal.TryParse(configuration[key], NumberStyles.Number, CultureInfo.InvariantCulture, out var value) && value > 0
            ? value
            : fallback;

    private static TransferCandidateDto MapTransferCandidate(ApplicationUser user)
        => new()
        {
            Id = user.Id,
            NomComplet = $"{user.Prenom} {user.Nom}".Trim(),
            Email = user.Email,
            Telephone = user.PhoneNumber,
            Matricule = user.Matricule
        };

    private async Task<MouvementPortefeuille?> LoadMovementAsync(Guid id, bool asNoTracking = false)
    {
        var query = db.MouvementsPortefeuilles
            .Include(m => m.PortefeuilleUtilisateur).ThenInclude(p => p.User)
            .Include(m => m.ValidePar)
            .Where(m => m.Id == id);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync();
    }

    private async Task LoadWalletActionDataAsync()
    {
        ViewBag.ComptePaiement = await db.ComptesPaiementMobile
            .Where(c => c.EstActif && !c.EstSupprime)
            .OrderByDescending(c => c.EstPrincipal)
            .ThenBy(c => c.Libelle)
            .FirstOrDefaultAsync();
        ViewBag.ComptesPaiement = await db.ComptesPaiementMobile
            .Include(c => c.Activite)
            .Where(c => c.EstActif && !c.EstSupprime)
            .OrderByDescending(c => c.EstPrincipal)
            .ThenBy(c => c.Libelle)
            .ToListAsync();
        ViewBag.ActivitesPaiement = await db.Activites
            .Where(a => !a.EstSupprime && (a.Statut == StatutActivite.Validee || a.Statut == StatutActivite.EnCours))
            .OrderByDescending(a => a.DateDebut)
            .Take(80)
            .ToListAsync();
    }

    private static void ApplyMovementToBalance(MouvementPortefeuille mouvement)
    {
        var wallet = mouvement.PortefeuilleUtilisateur;
        var before = wallet.Solde;
        switch (mouvement.Type)
        {
            case TypeMouvementPortefeuille.Credit:
            case TypeMouvementPortefeuille.Rechargement:
                wallet.Solde += mouvement.Montant;
                break;
            case TypeMouvementPortefeuille.Debit:
            case TypeMouvementPortefeuille.Paiement:
            case TypeMouvementPortefeuille.Don:
                if (wallet.Solde < mouvement.Montant)
                {
                    throw new InvalidOperationException("Solde insuffisant pour valider ce debit.");
                }
                wallet.Solde -= mouvement.Montant;
                break;
        }

        mouvement.SoldeAvant = before;
        mouvement.SoldeApres = wallet.Solde;
    }

    private async Task NotifyWalletOwnerAsync(MouvementPortefeuille mouvement, string subject, string body, bool includeReceiptLink)
    {
        var owner = mouvement.PortefeuilleUtilisateur.User
            ?? await db.Users.FirstOrDefaultAsync(u => u.Id == mouvement.PortefeuilleUtilisateur.UserId);
        if (owner is null || string.IsNullOrWhiteSpace(owner.Email))
        {
            return;
        }

        var link = includeReceiptLink ? BuildPublicReceiptLink(mouvement) : null;
        await emailService.SendAsync(
            owner.Email,
            subject,
            body,
            $"{owner.Prenom} {owner.Nom}".Trim(),
            "Portefeuille",
            link);
    }

    private string BuildPublicReceiptLink(MouvementPortefeuille mouvement)
    {
        var baseUrl = configuration["App:PublicBaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = $"{Request.Scheme}://{Request.Host}";
        }

        return $"{baseUrl}/Portefeuilles/RecuPublic/{mouvement.Id}?token={Uri.EscapeDataString(mouvement.RecuToken)}";
    }

    private FileContentResult BuildReceiptFile(MouvementPortefeuille mouvement)
    {
        var user = mouvement.PortefeuilleUtilisateur.User;
        var bytes = SimplePdfBuilder.BuildTextPdf(
            "Recu portefeuille - MANGO TAIKA",
            [
                $"Numero de recu : {mouvement.NumeroRecu}",
                $"Utilisateur : {user.Prenom} {user.Nom}",
                $"Type : {mouvement.Type}",
                $"Statut : {mouvement.Statut}",
                $"Montant : {mouvement.Montant:N0} {mouvement.Devise}",
                $"Reference : {mouvement.Reference ?? "-"}",
                $"Date demande : {mouvement.DateCreation.ToLocalTime():dd/MM/yyyy HH:mm}",
                $"Date validation : {mouvement.DateValidation?.ToLocalTime():dd/MM/yyyy HH:mm}",
                $"Solde avant : {mouvement.SoldeAvant?.ToString("N0") ?? "-"} {mouvement.Devise}",
                $"Solde apres : {mouvement.SoldeApres?.ToString("N0") ?? "-"} {mouvement.Devise}",
                $"Solde actuel : {mouvement.PortefeuilleUtilisateur.Solde:N0} {mouvement.PortefeuilleUtilisateur.Devise}",
                string.Empty,
                "District Scout MANGO TAIKA"
            ]);

        return File(bytes, "application/pdf", $"{mouvement.NumeroRecu ?? "recu-portefeuille"}.pdf");
    }

    private static string BuildWalletMessage(MouvementPortefeuille mouvement, string statut)
        => $"""
        Votre mouvement portefeuille a ete traite.
        Type : {mouvement.Type}
        Montant : {mouvement.Montant:N0} {mouvement.Devise}
        Reference : {mouvement.Reference ?? "-"}
        Statut : {statut}
        Solde apres operation : {mouvement.SoldeApres?.ToString("N0") ?? "-"} {mouvement.Devise}
        """;

    private static string? BuildPaymentComment(ComptePaiementMobile? compte, string? commentaire)
    {
        var parts = new List<string>();
        if (compte is not null)
        {
            parts.Add($"Compte impacte : {compte.Libelle} - {compte.Operateur} {compte.NumeroMobile}");
        }

        var normalizedComment = NormalizeOptional(commentaire);
        if (!string.IsNullOrWhiteSpace(normalizedComment))
        {
            parts.Add(normalizedComment);
        }

        return parts.Count == 0 ? null : string.Join(" | ", parts);
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeSearch(string? value)
    {
        var normalized = (value ?? string.Empty).Trim().Normalize(NormalizationForm.FormD);
        var chars = normalized
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray();
        return new string(chars).Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }
}
