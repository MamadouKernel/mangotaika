using System.ComponentModel.DataAnnotations;

namespace MangoTaika.DTOs;

public class BrancheDto
{
    public Guid Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public int? AgeMin { get; set; }
    public int? AgeMax { get; set; }
    public string? NomChefUnite { get; set; }
    public Guid? ChefUniteId { get; set; }
    public Guid GroupeId { get; set; }
    public string? NomGroupe { get; set; }
    public string? ResponsablePhotoUrl { get; set; }
    public int NombreScouts { get; set; }
    public int NombreFilles { get; set; }
    public int NombreGarcons { get; set; }
    public RepartitionMembresDto Jeunes { get; set; } = new();
    public RepartitionMembresDto Adultes { get; set; } = new();
    public List<BrancheGroupeSummaryDto> TotauxParGroupes { get; set; } = [];
    public List<BrancheMembreDto> Membres { get; set; } = [];
}

public class BrancheGroupeSummaryDto
{
    public Guid GroupeId { get; set; }
    public string NomGroupe { get; set; } = string.Empty;
    public string? LogoGroupeUrl { get; set; }
    public int NombreScouts { get; set; }
    public int NombreJeunes { get; set; }
    public int NombreAdultes { get; set; }
}

public class BrancheMembreDto
{
    public string Matricule { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public string Prenoms { get; set; } = string.Empty;
    public string Groupe { get; set; } = string.Empty;
    public string Fonction { get; set; } = "-";
}

public class BrancheCreateDto : IValidatableObject
{
    [Required(ErrorMessage = "Le nom de la branche est obligatoire.")]
    public string Nom { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public int? AgeMin { get; set; }
    public int? AgeMax { get; set; }
    [Required(ErrorMessage = "Le chef d'unité est obligatoire.")]
    public Guid? ChefUniteId { get; set; }
    public Guid GroupeId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (GroupeId == Guid.Empty)
        {
            yield return new ValidationResult(
                "Le groupe est obligatoire.",
                [nameof(GroupeId)]);
        }

        if (AgeMin.HasValue && AgeMin.Value < 0)
        {
            yield return new ValidationResult(
                "L'âge minimum ne peut pas être négatif.",
                [nameof(AgeMin)]);
        }

        if (AgeMax.HasValue && AgeMax.Value < 0)
        {
            yield return new ValidationResult(
                "L'âge maximum ne peut pas être négatif.",
                [nameof(AgeMax)]);
        }

        if (AgeMin.HasValue && AgeMax.HasValue && AgeMin > AgeMax)
        {
            yield return new ValidationResult(
                "L'âge minimum ne peut pas être supérieur à l'âge maximum.",
                [nameof(AgeMin), nameof(AgeMax)]);
        }
    }
}
