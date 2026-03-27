using System.ComponentModel.DataAnnotations;

namespace MangoTaika.DTOs;

public class BrancheDto
{
    public Guid Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? AgeMin { get; set; }
    public int? AgeMax { get; set; }
    public string? NomChefUnite { get; set; }
    public Guid? ChefUniteId { get; set; }
    public Guid GroupeId { get; set; }
    public string? NomGroupe { get; set; }
    public int NombreScouts { get; set; }
}

public class BrancheCreateDto : IValidatableObject
{
    public string Nom { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? AgeMin { get; set; }
    public int? AgeMax { get; set; }
    [Required(ErrorMessage = "Le chef d'unité est obligatoire.")]
    public Guid? ChefUniteId { get; set; }
    public Guid GroupeId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AgeMin.HasValue && AgeMax.HasValue && AgeMin > AgeMax)
        {
            yield return new ValidationResult(
                "L'âge minimum ne peut pas être supérieur à l'âge maximum.",
                [nameof(AgeMin), nameof(AgeMax)]);
        }
    }
}
