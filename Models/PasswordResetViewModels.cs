using System.ComponentModel.DataAnnotations;

namespace MangoTaika.Models;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "L'adresse email est requise.")]
    [EmailAddress(ErrorMessage = "Adresse email invalide.")]
    [Display(Name = "Adresse email")]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordViewModel
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le nouveau mot de passe est requis.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Le mot de passe doit contenir au moins 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nouveau mot de passe")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "La confirmation du mot de passe est requise.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Les mots de passe ne correspondent pas.")]
    [Display(Name = "Confirmer le mot de passe")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
