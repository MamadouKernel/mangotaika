using System.ComponentModel.DataAnnotations;

namespace MangoTaika.Models;

public class ActiverMfaViewModel
{
    public string Telephone { get; set; } = string.Empty;
    public bool CodeEnvoye { get; set; }

    [Required(ErrorMessage = "Le code de vérification est requis.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Le code doit contenir 6 chiffres.")]
    public string Code { get; set; } = string.Empty;
}

public class VerifierMfaViewModel
{
    [Required(ErrorMessage = "Le code est requis.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Le code doit contenir 6 chiffres.")]
    public string Code { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}
