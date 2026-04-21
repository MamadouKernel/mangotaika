using System.ComponentModel.DataAnnotations;

namespace MangoTaika.Models;

public class AdminUtilisateurDetailsViewModel
{
    public Guid Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telephone { get; set; }
    public List<string> Roles { get; set; } = [];
    public bool IsActive { get; set; }
    public DateTime DateCreation { get; set; }
    public string? NomGroupe { get; set; }
    public string? NomBranche { get; set; }
    public string? PhotoUrl { get; set; }
    public bool MfaActif { get; set; }
}

public class AdminUtilisateurEditViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Le nom est requis.")]
    [Display(Name = "Nom")]
    public string Nom { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le prénom est requis.")]
    [Display(Name = "Prénom")]
    public string Prenom { get; set; } = string.Empty;

    [EmailAddress]
    [Display(Name = "E-mail")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Le numéro est requis.")]
    [Display(Name = "Téléphone")]
    public string Telephone { get; set; } = string.Empty;

    [Display(Name = "Rôles")]
    public List<string> Roles { get; set; } = [];

    [Display(Name = "Compte actif")]
    public bool IsActive { get; set; } = true;

    [DataType(DataType.Password)]
    [Display(Name = "Nouveau mot de passe (optionnel)")]
    public string? NouveauMotDePasse { get; set; }

    [DataType(DataType.Password)]
    [Compare(nameof(NouveauMotDePasse), ErrorMessage = "La confirmation ne correspond pas.")]
    [Display(Name = "Confirmer le mot de passe")]
    public string? ConfirmationMotDePasse { get; set; }
}
