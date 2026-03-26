using System.ComponentModel.DataAnnotations;

namespace MangoTaika.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Le nom est requis.")]
    public string Nom { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le prénom est requis.")]
    public string Prenom { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le numéro de téléphone est requis.")]
    [Phone(ErrorMessage = "Numéro de téléphone invalide.")]
    [Display(Name = "Téléphone")]
    public string Telephone { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email invalide.")]
    [Display(Name = "Email (optionnel)")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Le mot de passe est requis.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Le mot de passe doit contenir au moins 8 caractères.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Les mots de passe ne correspondent pas.")]
    [Display(Name = "Confirmer le mot de passe")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "Matricule(s) de vos enfants")]
    public string? Matricules { get; set; }

    [Display(Name = "Code d'invitation gestionnaire")]
    public string? CodeInvitation { get; set; }

    [Display(Name = "Mon matricule scout")]
    public string? MatriculeScout { get; set; }

    [Required(ErrorMessage = "Le rôle est requis.")]
    [Display(Name = "Vous êtes")]
    public string Role { get; set; } = "Parent";

    [Range(typeof(bool), "true", "true", ErrorMessage = "Vous devez accepter la politique de confidentialité.")]
    public bool Consentement { get; set; }
}
