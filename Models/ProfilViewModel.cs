using System.ComponentModel.DataAnnotations;

namespace MangoTaika.Models;

public class ProfilViewModel
{
    [Required(ErrorMessage = "Le nom est requis.")]
    public string Nom { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le prénom est requis.")]
    public string Prenom { get; set; } = string.Empty;

    public string? Email { get; set; }

    [Phone(ErrorMessage = "Numéro de téléphone invalide.")]
    public string Telephone { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string? Role { get; set; }
    public string? NomGroupe { get; set; }
    public string? NomBranche { get; set; }
    public DateTime DateCreation { get; set; }
}

public class ChangerMotDePasseViewModel
{
    [Required(ErrorMessage = "L'ancien mot de passe est requis.")]
    [DataType(DataType.Password)]
    public string AncienMotDePasse { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le nouveau mot de passe est requis.")]
    [DataType(DataType.Password)]
    [MinLength(8, ErrorMessage = "Le mot de passe doit contenir au moins 8 caractères.")]
    public string NouveauMotDePasse { get; set; } = string.Empty;

    [Required(ErrorMessage = "La confirmation est requise.")]
    [DataType(DataType.Password)]
    [Compare("NouveauMotDePasse", ErrorMessage = "Les mots de passe ne correspondent pas.")]
    public string ConfirmerMotDePasse { get; set; } = string.Empty;
}
