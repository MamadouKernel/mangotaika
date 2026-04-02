namespace MangoTaika.DTOs;

using System.ComponentModel.DataAnnotations;
using MangoTaika.Helpers;
using System.Text.Json.Serialization;

public class ScoutDto
{
    public Guid Id { get; set; }
    public string? Matricule { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public DateTime DateNaissance { get; set; }
    public string? LieuNaissance { get; set; }
    public string? Sexe { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    public string? RegionScoute { get; set; }
    public string? District { get; set; }
    public string? NumeroCarte { get; set; }
    public string? Fonction { get; set; }
    public string? FonctionVieActive { get; set; }
    public string? NiveauFormationScoute { get; set; }
    public string? ContactUrgenceNom { get; set; }
    public string? ContactUrgenceRelation { get; set; }
    public string? ContactUrgenceTelephone { get; set; }
    public string? StatutASCCI { get; set; }
    public bool AssuranceAnnuelle { get; set; }
    public string? AdresseGeographique { get; set; }
    public Guid? GroupeId { get; set; }
    public Guid? BrancheId { get; set; }
    public string? NomGroupe { get; set; }
    public string? NomBranche { get; set; }
    public string? DerniereInscriptionAnnuelle { get; set; }
    public bool? DerniereCotisationNationaleAjour { get; set; }
    public int HistoriqueInscriptionsCount { get; set; }
}

public class ScoutCreateDto : IValidatableObject
{
    [RegularExpression(ScoutMatriculeFormat.Pattern, ErrorMessage = ScoutMatriculeFormat.ErrorMessage)]
    public string? Matricule { get; set; }

    [Required(ErrorMessage = "Le nom est requis.")]
    public string Nom { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le prenom est requis.")]
    public string Prenom { get; set; } = string.Empty;

    [Required(ErrorMessage = "La date de naissance est requise.")]
    public DateTime DateNaissance { get; set; }
    public string? LieuNaissance { get; set; }
    public string? Sexe { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    public string? RegionScoute { get; set; }
    public string? District { get; set; }
    public string? NumeroCarte { get; set; }
    public string? Fonction { get; set; }
    public string? FonctionVieActive { get; set; }
    public string? NiveauFormationScoute { get; set; }
    public string? ContactUrgenceNom { get; set; }
    public string? ContactUrgenceRelation { get; set; }
    public string? ContactUrgenceTelephone { get; set; }
    public bool AssuranceAnnuelle { get; set; }
    public string? AdresseGeographique { get; set; }
    public Guid? GroupeId { get; set; }
    public Guid? BrancheId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateNaissance == default)
        {
            yield return new ValidationResult(
                "La date de naissance est requise.",
                [nameof(DateNaissance)]);
        }
        else if (DateNaissance.Date > DateTime.UtcNow.Date)
        {
            yield return new ValidationResult(
                "La date de naissance ne peut pas etre dans le futur.",
                [nameof(DateNaissance)]);
        }

        if (BrancheId.HasValue && !GroupeId.HasValue)
        {
            yield return new ValidationResult(
                "Le groupe est obligatoire lorsqu'une branche est selectionnee.",
                [nameof(GroupeId), nameof(BrancheId)]);
        }
    }
}

public class ScoutImportResultDto
{
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> CreatedMatricules { get; set; } = [];
    public List<string> UpdatedMatricules { get; set; } = [];
    public List<ScoutImportErrorDto> Errors { get; set; } = [];
}

public class ScoutImportErrorDto
{
    public int LineNumber { get; set; }
    public string? Matricule { get; set; }
    public string Message { get; set; } = string.Empty;

    [JsonIgnore]
    public string DisplayMessage => string.IsNullOrWhiteSpace(Matricule)
        ? $"Ligne {LineNumber}: {Message}"
        : $"Ligne {LineNumber} ({Matricule}): {Message}";
}
