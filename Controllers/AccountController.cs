using System.Text;
using System.Text.Json;
using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Helpers;
using MangoTaika.Models;
using MangoTaika.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Controllers;

public class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    AppDbContext db,
    ISmsService smsService) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        // Trouver l'utilisateur par téléphone
        var user = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == model.Telephone);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Identifiants invalides.");
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Votre compte est en attente d'activation par un administrateur.");
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
            return LocalRedirect(returnUrl ?? "/Dashboard");
        if (result.RequiresTwoFactor)
            return RedirectToAction(nameof(VerifierMfa), new { rememberMe = model.RememberMe });
        if (result.IsLockedOut)
            ModelState.AddModelError(string.Empty, "Compte verrouillé. Réessayez plus tard.");
        else
            ModelState.AddModelError(string.Empty, "Identifiants invalides.");
        return View(model);
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        // Seuls Parent, Gestionnaire et Scout peuvent s'inscrire
        if (model.Role != "Parent" && model.Role != "Gestionnaire" && model.Role != "Scout")
        {
            ModelState.AddModelError("Role", "Rôle invalide.");
            return View(model);
        }

        // Vérifier unicité du téléphone
        if (await db.Users.AnyAsync(u => u.PhoneNumber == model.Telephone))
        {
            ModelState.AddModelError("Telephone", "Ce numéro de téléphone est déjà utilisé.");
            return View(model);
        }

        // === Validation selon le rôle ===

        // Parent / Tuteur : matricule(s) obligatoire(s)
        if (model.Role == "Parent")
        {
            if (string.IsNullOrWhiteSpace(model.Matricules))
            {
                ModelState.AddModelError("Matricules", "En tant que parent / tuteur, vous devez fournir le(s) matricule(s) de vos enfants.");
                return View(model);
            }
        }

        // Gestionnaire : code d'invitation obligatoire
        Data.Entities.CodeInvitation? codeInvitation = null;
        if (model.Role == "Gestionnaire")
        {
            if (string.IsNullOrWhiteSpace(model.CodeInvitation))
            {
                ModelState.AddModelError("CodeInvitation", "Un code d'invitation est requis pour s'inscrire en tant que gestionnaire.");
                return View(model);
            }
            codeInvitation = await db.CodesInvitation
                .FirstOrDefaultAsync(c => c.Code == model.CodeInvitation.Trim() && !c.EstUtilise);
            if (codeInvitation is null)
            {
                ModelState.AddModelError("CodeInvitation", "Code d'invitation invalide ou déjà utilisé.");
                return View(model);
            }
        }

        // Scout : matricule obligatoire et doit exister en base
        Scout? scoutLie = null;
        if (model.Role == "Scout")
        {
            if (string.IsNullOrWhiteSpace(model.MatriculeScout))
            {
                ModelState.AddModelError("MatriculeScout", "Votre matricule scout est requis.");
                return View(model);
            }
            scoutLie = await db.Scouts.FirstOrDefaultAsync(s => s.Matricule == model.MatriculeScout.Trim() && s.IsActive);
            if (scoutLie is null)
            {
                ModelState.AddModelError("MatriculeScout", "Matricule introuvable. Vérifiez auprès de votre chef de groupe.");
                return View(model);
            }
        }

        // Valider les matricules si fournis
        var scouts = new List<Scout>();
        if (!string.IsNullOrWhiteSpace(model.Matricules))
        {
            var matricules = model.Matricules
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var mat in matricules)
            {
                var scout = await db.Scouts.FirstOrDefaultAsync(s => s.Matricule == mat && s.IsActive);
                if (scout == null)
                {
                    ModelState.AddModelError("Matricules", $"Matricule introuvable : {mat}");
                    return View(model);
                }
                scouts.Add(scout);
            }
        }

        var user = new ApplicationUser
        {
            UserName = model.Telephone,
            PhoneNumber = model.Telephone,
            Email = model.Email,
            Nom = model.Nom,
            Prenom = model.Prenom,
            IsActive = false
        };
        var result = await userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, model.Role);

            // Lier les scouts au parent
            if (scouts.Count > 0)
            {
                var parent = new Parent
                {
                    Id = Guid.NewGuid(),
                    Nom = model.Nom,
                    Prenom = model.Prenom,
                    Telephone = model.Telephone,
                    Email = model.Email
                };
                parent.Scouts = scouts;
                db.Parents.Add(parent);
                await db.SaveChangesAsync();
            }

            // Lier le scout à son compte utilisateur
            if (scoutLie is not null)
            {
                scoutLie.UserId = user.Id;
                await db.SaveChangesAsync();
            }

            // Marquer le code d'invitation comme utilisé
            if (codeInvitation is not null)
            {
                codeInvitation.EstUtilise = true;
                codeInvitation.DateUtilisation = DateTime.UtcNow;
                codeInvitation.UtilisePaId = user.Id;
                await db.SaveChangesAsync();
            }

            TempData["Success"] = "Inscription réussie. Votre compte sera activé par un administrateur.";
            return RedirectToAction(nameof(Login));
        }
        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    // === PROFIL UTILISATEUR ===

    [Authorize]
    public async Task<IActionResult> Profil()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction(nameof(Login));

        await db.Entry(user).Reference(u => u.Groupe).LoadAsync();
        await db.Entry(user).Reference(u => u.Branche).LoadAsync();
        var roles = await userManager.GetRolesAsync(user);

        var model = new ProfilViewModel
        {
            Nom = user.Nom,
            Prenom = user.Prenom,
            Email = user.Email,
            Telephone = user.PhoneNumber ?? "",
            PhotoUrl = user.PhotoUrl,
            Role = roles.FirstOrDefault(),
            NomGroupe = user.Groupe?.Nom,
            NomBranche = user.Branche?.Nom,
            DateCreation = user.DateCreation
        };
        ViewBag.MfaActif = await userManager.GetTwoFactorEnabledAsync(user);
        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profil(ProfilViewModel model, IFormFile? Photo)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction(nameof(Login));

        user.Nom = model.Nom;
        user.Prenom = model.Prenom;
        user.Email = model.Email;

        // Droit de rectification : permettre le changement de téléphone
        if (!string.IsNullOrWhiteSpace(model.Telephone) && model.Telephone != user.PhoneNumber)
        {
            // Vérifier unicité
            if (await db.Users.AnyAsync(u => u.PhoneNumber == model.Telephone && u.Id != user.Id))
            {
                ModelState.AddModelError("Telephone", "Ce numéro de téléphone est déjà utilisé.");
                return View(model);
            }
            user.PhoneNumber = model.Telephone;
            user.UserName = model.Telephone;
            user.NormalizedUserName = model.Telephone.ToUpperInvariant();
        }

        if (Photo is not null && Photo.Length > 0)
        {
            var dir = Path.Combine("wwwroot", "uploads", "profils");
            Directory.CreateDirectory(dir);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(Photo.FileName)}";
            using var stream = new FileStream(Path.Combine(dir, fileName), FileMode.Create);
            await Photo.CopyToAsync(stream);
            user.PhotoUrl = $"/uploads/profils/{fileName}";
        }

        await userManager.UpdateAsync(user);
        TempData["Success"] = "Profil mis à jour.";
        return RedirectToAction(nameof(Profil));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangerMotDePasse(ChangerMotDePasseViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMdp"] = "Veuillez corriger les erreurs.";
            return RedirectToAction(nameof(Profil));
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction(nameof(Login));

        var result = await userManager.ChangePasswordAsync(user, model.AncienMotDePasse, model.NouveauMotDePasse);
        if (result.Succeeded)
        {
            await signInManager.RefreshSignInAsync(user);
            TempData["Success"] = "Mot de passe modifié avec succès.";
        }
        else
        {
            TempData["ErrorMdp"] = string.Join(" ", result.Errors.Select(e => e.Description));
        }
        return RedirectToAction(nameof(Profil));
    }

    // === GESTION DES COMPTES (Admin/Gestionnaire) ===

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> Utilisateurs()
    {
        var (page, ps) = ListPagination.Read(Request);
        var ordered = db.Users.OrderByDescending(u => u.DateCreation);
        var total = await ordered.CountAsync();
        var (p, pageSize, skip, totalPages) = ListPagination.Normalize(page, ps, total);
        var users = await ordered.Skip(skip).Take(pageSize).ToListAsync();

        var userRoles = new Dictionary<Guid, string>();
        foreach (var u in users)
        {
            var roles = await userManager.GetRolesAsync(u);
            userRoles[u.Id] = roles.FirstOrDefault() ?? "—";
        }
        ViewBag.UserRoles = userRoles;
        ListPagination.SetViewData(ViewData, HttpContext, p, pageSize, total, totalPages);
        return View(users);
    }

    private bool EstGestionnaireSansAdmin() =>
        User.IsInRole("Gestionnaire") && !User.IsInRole("Administrateur");

    private async Task<bool> EstUtilisateurAdminAsync(ApplicationUser u) =>
        (await userManager.GetRolesAsync(u)).Contains("Administrateur");

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> UtilisateurDetails(Guid id)
    {
        var user = await db.Users
            .Include(u => u.Groupe)
            .Include(u => u.Branche)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();
        if (EstGestionnaireSansAdmin() && await EstUtilisateurAdminAsync(user))
            return Forbid();

        var roles = await userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "—";
        if (role == "Parent") role = "Parent / Tuteur";

        var model = new AdminUtilisateurDetailsViewModel
        {
            Id = user.Id,
            Nom = user.Nom,
            Prenom = user.Prenom,
            Email = user.Email,
            Telephone = user.PhoneNumber,
            Role = role,
            IsActive = user.IsActive,
            DateCreation = user.DateCreation,
            NomGroupe = user.Groupe?.Nom,
            NomBranche = user.Branche?.Nom,
            PhotoUrl = user.PhotoUrl,
            MfaActif = await userManager.GetTwoFactorEnabledAsync(user)
        };
        return View(model);
    }

    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<IActionResult> EditerUtilisateur(Guid id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();
        if (EstGestionnaireSansAdmin() && await EstUtilisateurAdminAsync(user))
            return Forbid();

        var roles = await userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Scout";

        var model = new AdminUtilisateurEditViewModel
        {
            Id = user.Id,
            Nom = user.Nom,
            Prenom = user.Prenom,
            Email = user.Email,
            Telephone = user.PhoneNumber ?? user.UserName ?? "",
            Role = role,
            IsActive = user.IsActive
        };
        ViewBag.RolesDisponibles = RolesPourEdition();
        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditerUtilisateur(Guid id, AdminUtilisateurEditViewModel model)
    {
        if (id != model.Id) return BadRequest();

        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();
        if (EstGestionnaireSansAdmin() && await EstUtilisateurAdminAsync(user))
            return Forbid();

        ViewBag.RolesDisponibles = RolesPourEdition();

        if (EstGestionnaireSansAdmin() && model.Role == "Administrateur")
        {
            ModelState.AddModelError(nameof(model.Role), "Vous ne pouvez pas attribuer le rôle Administrateur.");
        }

        var currentUserId = Guid.Parse(userManager.GetUserId(User)!);
        if (id == currentUserId && !model.IsActive)
            ModelState.AddModelError(nameof(model.IsActive), "Vous ne pouvez pas désactiver votre propre compte.");

        if (!string.IsNullOrWhiteSpace(model.NouveauMotDePasse))
        {
            if (model.NouveauMotDePasse != model.ConfirmationMotDePasse)
                ModelState.AddModelError(nameof(model.ConfirmationMotDePasse), "La confirmation ne correspond pas.");
        }

        if (!ModelState.IsValid) return View(model);

        if (!string.IsNullOrWhiteSpace(model.Telephone) && model.Telephone != user.PhoneNumber)
        {
            if (await db.Users.AnyAsync(u => u.PhoneNumber == model.Telephone && u.Id != user.Id))
            {
                ModelState.AddModelError(nameof(model.Telephone), "Ce numéro est déjà utilisé.");
                return View(model);
            }
            user.PhoneNumber = model.Telephone;
            user.UserName = model.Telephone;
            user.NormalizedUserName = model.Telephone.ToUpperInvariant();
        }

        user.Nom = model.Nom;
        user.Prenom = model.Prenom;
        user.Email = model.Email;
        user.IsActive = model.IsActive;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var e in updateResult.Errors)
                ModelState.AddModelError(string.Empty, e.Description);
            return View(model);
        }

        var anciensRoles = await userManager.GetRolesAsync(user);
        if (!anciensRoles.Contains(model.Role))
        {
            await userManager.RemoveFromRolesAsync(user, anciensRoles);
            var addRole = await userManager.AddToRoleAsync(user, model.Role);
            if (!addRole.Succeeded)
            {
                foreach (var e in addRole.Errors)
                    ModelState.AddModelError(nameof(model.Role), e.Description);
                await userManager.AddToRolesAsync(user, anciensRoles);
                return View(model);
            }
        }

        if (!string.IsNullOrWhiteSpace(model.NouveauMotDePasse))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var pwdResult = await userManager.ResetPasswordAsync(user, token, model.NouveauMotDePasse);
            if (!pwdResult.Succeeded)
            {
                foreach (var e in pwdResult.Errors)
                    ModelState.AddModelError(nameof(model.NouveauMotDePasse), e.Description);
                return View(model);
            }
        }

        if (!model.IsActive)
            await userManager.UpdateSecurityStampAsync(user);

        TempData["Success"] = "Utilisateur mis à jour.";
        return RedirectToAction(nameof(UtilisateurDetails), new { id });
    }

    private IReadOnlyList<string> RolesPourEdition()
    {
        if (EstGestionnaireSansAdmin())
            return ["Gestionnaire", "AgentSupport", "Superviseur", "Scout", "Parent", "Consultant"];
        return ["Administrateur", "Gestionnaire", "AgentSupport", "Superviseur", "Scout", "Parent", "Consultant"];
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivateUser(Guid id)
    {
        var user = await db.Users.FindAsync(id);
        if (user != null)
        {
            user.IsActive = true;
            await db.SaveChangesAsync();
            TempData["Success"] = $"Compte de {user.Prenom} {user.Nom} activé.";
        }
        return RedirectToAction(nameof(Utilisateurs));
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateUser(Guid id)
    {
        var user = await db.Users.FindAsync(id);
        if (user != null)
        {
            user.IsActive = false;
            // Invalider toutes les sessions actives de cet utilisateur
            await userManager.UpdateSecurityStampAsync(user);
            await db.SaveChangesAsync();
            TempData["Success"] = $"Compte de {user.Prenom} {user.Nom} désactivé.";
        }
        return RedirectToAction(nameof(Utilisateurs));
    }

    // === CODES D'INVITATION GESTIONNAIRE ===

    [Authorize(Roles = "Administrateur")]
    public async Task<IActionResult> CodesInvitation()
    {
        var (page, ps) = ListPagination.Read(Request);
        var query = db.CodesInvitation
            .Include(c => c.Createur)
            .Include(c => c.UtilisePar)
            .OrderByDescending(c => c.DateCreation);
        var total = await query.CountAsync();
        var (p, pageSize, skip, totalPages) = ListPagination.Normalize(page, ps, total);
        var codes = await query.Skip(skip).Take(pageSize).ToListAsync();
        ListPagination.SetViewData(ViewData, HttpContext, p, pageSize, total, totalPages);
        return View(codes);
    }

    [Authorize(Roles = "Administrateur")]
    public async Task<IActionResult> DetailsCodeInvitation(Guid id)
    {
        var c = await db.CodesInvitation
            .Include(x => x.Createur)
            .Include(x => x.UtilisePar)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();
        return View(c);
    }

    [HttpPost]
    [Authorize(Roles = "Administrateur")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenererCode()
    {
        var userId = Guid.Parse(userManager.GetUserId(User)!);
        var code = new Data.Entities.CodeInvitation
        {
            Id = Guid.NewGuid(),
            Code = $"MT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            CreateurId = userId
        };
        db.CodesInvitation.Add(code);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Code généré : {code.Code}";
        return RedirectToAction(nameof(CodesInvitation));
    }

    // === MFA SMS (Authentification à deux facteurs par SMS) ===

    [Authorize]
    public async Task<IActionResult> ActiverMfa()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction(nameof(Login));

        var model = new ActiverMfaViewModel
        {
            Telephone = user.PhoneNumber ?? "",
            CodeEnvoye = false
        };
        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnvoyerCodeMfa()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction(nameof(Login));

        var code = await userManager.GenerateChangePhoneNumberTokenAsync(user, user.PhoneNumber!);
        await smsService.SendSmsAsync(user.PhoneNumber!, $"MANGO TAÏKA - Votre code de vérification : {code}");

        TempData["MfaCodeEnvoye"] = true;
        return RedirectToAction(nameof(ActiverMfa));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActiverMfa(ActiverMfaViewModel model)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction(nameof(Login));

        model.Telephone = user.PhoneNumber ?? "";

        if (string.IsNullOrWhiteSpace(model.Code))
        {
            ModelState.AddModelError("Code", "Le code est requis.");
            model.CodeEnvoye = true;
            return View(model);
        }

        var isValid = await userManager.VerifyChangePhoneNumberTokenAsync(user, model.Code.Trim(), user.PhoneNumber!);
        if (!isValid)
        {
            ModelState.AddModelError("Code", "Code de vérification invalide ou expiré.");
            model.CodeEnvoye = true;
            return View(model);
        }

        await userManager.SetTwoFactorEnabledAsync(user, true);
        user.PhoneNumberConfirmed = true;
        await userManager.UpdateAsync(user);
        TempData["Success"] = "Authentification à deux facteurs par SMS activée.";
        return RedirectToAction(nameof(Profil));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DesactiverMfa()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction(nameof(Login));
        await userManager.SetTwoFactorEnabledAsync(user, false);
        TempData["Success"] = "Authentification à deux facteurs désactivée.";
        return RedirectToAction(nameof(Profil));
    }

    [HttpGet]
    public IActionResult VerifierMfa(bool rememberMe = false)
    {
        return View(new VerifierMfaViewModel { RememberMe = rememberMe });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnvoyerCodeLogin()
    {
        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null) return RedirectToAction(nameof(Login));

        var code = await userManager.GenerateTwoFactorTokenAsync(user, "Phone");
        await smsService.SendSmsAsync(user.PhoneNumber!, $"MANGO TAÏKA - Votre code de connexion : {code}");

        TempData["SmsEnvoye"] = true;
        return RedirectToAction(nameof(VerifierMfa));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifierMfa(VerifierMfaViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await signInManager.TwoFactorSignInAsync("Phone", model.Code.Trim(), model.RememberMe, rememberClient: true);

        if (result.Succeeded)
            return RedirectToAction("Index", "Dashboard");
        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Compte verrouillé. Réessayez plus tard.");
            return View(model);
        }

        ModelState.AddModelError("Code", "Code invalide ou expiré.");
        return View(model);
    }

    // === ESPACE SCOUT : Ma fiche ===

    [Authorize(Roles = "Scout")]
    public async Task<IActionResult> MaFiche()
    {
        var userId = Guid.Parse(userManager.GetUserId(User)!);
        var scout = await db.Scouts
            .Include(s => s.Groupe)
            .Include(s => s.Branche)
            .Include(s => s.SuivisAcademiques)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive);

        if (scout is null)
        {
            TempData["Error"] = "Aucune fiche scout n'est liée à votre compte.";
            return RedirectToAction(nameof(Profil));
        }

        var competences = await db.Competences.Where(c => c.ScoutId == scout.Id).ToListAsync();
        var participations = await db.ParticipantsActivite
            .Include(p => p.Activite)
            .Where(p => p.ScoutId == scout.Id)
            .ToListAsync();
        var cotisations = await db.TransactionsFinancieres
            .Where(t => t.ScoutId == scout.Id && !t.EstSupprime)
            .OrderByDescending(t => t.DateTransaction)
            .ToListAsync();
        var historique = await db.HistoriqueFonctions
            .Where(h => h.ScoutId == scout.Id)
            .OrderByDescending(h => h.DateDebut)
            .ToListAsync();

        ViewBag.Competences = competences;
        ViewBag.Participations = participations;
        ViewBag.Cotisations = cotisations;
        ViewBag.Historique = historique;
        ViewBag.PeutEditer = true;
        return View(scout);
    }

    [Authorize(Roles = "Scout")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ModifierMaFiche(string? Telephone, string? Email)
    {
        var userId = Guid.Parse(userManager.GetUserId(User)!);
        var scout = await db.Scouts.FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive);
        if (scout is null) return RedirectToAction(nameof(Profil));

        scout.Telephone = Telephone;
        scout.Email = Email;
        await db.SaveChangesAsync();

        TempData["Success"] = "Fiche mise à jour.";
        return RedirectToAction(nameof(MaFiche));
    }

    // === ESPACE PARENT : Mes enfants ===

    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> MesEnfants()
    {
        var userId = Guid.Parse(userManager.GetUserId(User)!);
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return RedirectToAction(nameof(Login));

        // Trouver le parent lié
        var parent = await db.Parents
            .Include(p => p.Scouts).ThenInclude(s => s.Groupe)
            .Include(p => p.Scouts).ThenInclude(s => s.Branche)
            .FirstOrDefaultAsync(p => p.Telephone == user.PhoneNumber);

        if (parent is null || !parent.Scouts.Any())
        {
            TempData["Error"] = "Aucun enfant n'est lié à votre compte.";
            return RedirectToAction(nameof(Profil));
        }

        return View(parent.Scouts.Where(s => s.IsActive).ToList());
    }

    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> FicheEnfant(Guid id)
    {
        var userId = Guid.Parse(userManager.GetUserId(User)!);
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return RedirectToAction(nameof(Login));

        // Vérifier que le scout est bien l'enfant du parent
        var parent = await db.Parents
            .Include(p => p.Scouts)
            .FirstOrDefaultAsync(p => p.Telephone == user.PhoneNumber);

        if (parent is null || !parent.Scouts.Any(s => s.Id == id))
            return Forbid();

        var scout = await db.Scouts
            .Include(s => s.Groupe)
            .Include(s => s.Branche)
            .Include(s => s.SuivisAcademiques)
            .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

        if (scout is null) return NotFound();

        var competences = await db.Competences.Where(c => c.ScoutId == scout.Id).ToListAsync();
        var participations = await db.ParticipantsActivite
            .Include(p => p.Activite)
            .Where(p => p.ScoutId == scout.Id)
            .ToListAsync();
        var cotisations = await db.TransactionsFinancieres
            .Where(t => t.ScoutId == scout.Id && !t.EstSupprime)
            .OrderByDescending(t => t.DateTransaction)
            .ToListAsync();

        ViewBag.Competences = competences;
        ViewBag.Participations = participations;
        ViewBag.Cotisations = cotisations;
        return View("MaFiche", scout); // Réutilise la même vue que MaFiche
    }

    // === DROITS RGPD / ARTCI ===

    /// <summary>
    /// Droit d'accès : Télécharger toutes les données personnelles de l'utilisateur (JSON)
    /// </summary>
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TelechargerMesDonnees()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction(nameof(Login));

        var userId = user.Id;
        var roles = await userManager.GetRolesAsync(user);

        // Collecter toutes les données liées à l'utilisateur
        var donnees = new Dictionary<string, object?>
        {
            ["InformationsPersonnelles"] = new
            {
                user.Nom,
                user.Prenom,
                user.Email,
                Telephone = user.PhoneNumber,
                user.PhotoUrl,
                user.Matricule,
                user.IsActive,
                user.DateCreation,
                Roles = roles.ToList(),
                MfaActif = await userManager.GetTwoFactorEnabledAsync(user)
            }
        };

        // Groupe et branche
        await db.Entry(user).Reference(u => u.Groupe).LoadAsync();
        await db.Entry(user).Reference(u => u.Branche).LoadAsync();
        donnees["Groupe"] = user.Groupe != null ? new { user.Groupe.Nom, user.Groupe.Adresse } : null;
        donnees["Branche"] = user.Branche != null ? new { user.Branche.Nom } : null;

        // Scout lié (si le user est un scout)
        var scoutLie = await db.Scouts.Include(s => s.Groupe).Include(s => s.Branche)
            .FirstOrDefaultAsync(s => s.UserId == userId);
        if (scoutLie != null)
        {
            donnees["ProfilScout"] = new
            {
                scoutLie.Nom,
                scoutLie.Prenom,
                scoutLie.Matricule,
                scoutLie.NumeroCarte,
                scoutLie.DateNaissance,
                scoutLie.Telephone,
                scoutLie.Email,
                scoutLie.RegionScoute,
                scoutLie.District,
                scoutLie.Fonction,
                Groupe = scoutLie.Groupe?.Nom,
                Branche = scoutLie.Branche?.Nom
            };

            // Compétences du scout
            var competences = await db.Competences
                .Where(c => c.ScoutId == scoutLie.Id)
                .Select(c => new { c.Nom, c.Niveau, c.DateObtention })
                .ToListAsync();
            donnees["Competences"] = competences;

            // Suivi académique
            var suivis = await db.SuivisAcademiques
                .Where(s => s.ScoutId == scoutLie.Id)
                .Select(s => new { s.AnneeScolaire, s.Etablissement, s.NiveauScolaire, s.Classe, s.MoyenneGenerale, s.Mention })
                .ToListAsync();
            donnees["SuiviAcademique"] = suivis;

            // Participations aux activités
            var participations = await db.ParticipantsActivite
                .Include(p => p.Activite)
                .Where(p => p.ScoutId == scoutLie.Id)
                .Select(p => new { Activite = p.Activite!.Titre, p.Activite.DateDebut, p.Activite.Lieu, p.Presence })
                .ToListAsync();
            donnees["ParticipationsActivites"] = participations;

            // Cotisations
            var cotisations = await db.TransactionsFinancieres
                .Where(t => t.ScoutId == scoutLie.Id && !t.EstSupprime)
                .Select(t => new { t.Libelle, t.Montant, t.Type, t.DateTransaction })
                .ToListAsync();
            donnees["Cotisations"] = cotisations;
        }

        // Tickets créés
        var tickets = await db.Tickets
            .Where(t => t.CreateurId == userId && !t.EstSupprime)
            .Select(t => new { t.Sujet, t.Description, t.Statut, t.Priorite, t.DateCreation })
            .ToListAsync();
        donnees["Tickets"] = tickets;

        // Historique de fonctions
        var historique = await db.HistoriqueFonctions
            .Where(h => h.UserId == userId)
            .Select(h => new { h.Fonction, h.DateDebut, h.DateFin, h.Commentaire })
            .ToListAsync();
        donnees["HistoriqueFonctions"] = historique;

        // Demandes d'autorisation créées
        var demandes = await db.DemandesAutorisation
            .Where(d => d.DemandeurId == userId)
            .Select(d => new { d.Titre, d.TypeActivite, d.Statut, d.DateCreation, d.DateActivite })
            .ToListAsync();
        donnees["DemandesAutorisation"] = demandes;

        var json = JsonSerializer.Serialize(donnees, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        var bytes = Encoding.UTF8.GetBytes(json);
        return File(bytes, "application/json", $"mes-donnees-mangotaika-{DateTime.UtcNow:yyyyMMdd}.json");
    }

    /// <summary>
    /// Droit à l'oubli : Demander la suppression de son compte et l'anonymisation des données
    /// </summary>
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SupprimerMonCompte(string MotDePasseConfirmation)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction(nameof(Login));

        // Vérifier le mot de passe pour confirmer l'identité
        if (string.IsNullOrWhiteSpace(MotDePasseConfirmation) ||
            !await userManager.CheckPasswordAsync(user, MotDePasseConfirmation))
        {
            TempData["ErrorSuppression"] = "Mot de passe incorrect. La suppression a été annulée.";
            return RedirectToAction(nameof(Profil));
        }

        var userId = user.Id;
        var anonyme = $"SUPPRIME-{Guid.NewGuid().ToString("N")[..8]}";

        // 1. Anonymiser le scout lié
        var scoutLie = await db.Scouts.FirstOrDefaultAsync(s => s.UserId == userId);
        if (scoutLie != null)
        {
            scoutLie.UserId = null;
            // On ne supprime pas le scout (données associatives), mais on coupe le lien
        }

        // 2. Anonymiser les tickets (garder pour historique mais anonymiser)
        var tickets = await db.Tickets.Where(t => t.CreateurId == userId).ToListAsync();
        foreach (var t in tickets)
        {
            t.EstSupprime = true;
        }

        // 3. Anonymiser les messages de tickets
        var messages = await db.MessagesTicket.Where(m => m.AuteurId == userId).ToListAsync();
        foreach (var m in messages)
        {
            m.Contenu = "[Message supprimé - Droit à l'oubli]";
        }

        // 4. Anonymiser les commentaires d'activités
        var commentaires = await db.CommentairesActivite.Where(c => c.AuteurId == userId).ToListAsync();
        foreach (var c in commentaires)
        {
            c.Contenu = "[Commentaire supprimé - Droit à l'oubli]";
        }

        // 5. Marquer les demandes d'autorisation
        var demandes = await db.DemandesAutorisation.Where(d => d.DemandeurId == userId).ToListAsync();
        foreach (var d in demandes)
        {
            d.Observations = "[Demandeur supprimé - Droit à l'oubli]";
        }

        // 6. Anonymiser les données du compte utilisateur
        user.Nom = "Utilisateur";
        user.Prenom = "Supprimé";
        user.Email = $"{anonyme}@supprime.local";
        user.NormalizedEmail = user.Email.ToUpperInvariant();
        user.PhoneNumber = null;
        user.UserName = anonyme;
        user.NormalizedUserName = anonyme.ToUpperInvariant();
        user.PhotoUrl = null;
        user.Matricule = null;
        user.IsActive = false;
        user.PasswordHash = null; // Empêcher toute connexion
        user.SecurityStamp = Guid.NewGuid().ToString(); // Invalider toutes les sessions
        user.TwoFactorEnabled = false;
        user.PhoneNumberConfirmed = false;
        user.EmailConfirmed = false;
        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.MaxValue; // Verrouiller définitivement

        await db.SaveChangesAsync();

        // Déconnecter l'utilisateur
        await signInManager.SignOutAsync();

        TempData["Success"] = "Votre compte a été supprimé et vos données personnelles ont été anonymisées conformément à la loi n°2013-450.";
        return RedirectToAction("Index", "Home");
    }
}
