using System.ComponentModel.DataAnnotations;

namespace MangoTaika.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Le numéro de téléphone est requis.")]
    [Phone(ErrorMessage = "Numéro de téléphone invalide.")]
    [Display(Name = "Téléphone")]
    public string Telephone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le mot de passe est requis.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Se souvenir de moi")]
    public bool RememberMe { get; set; }
}
