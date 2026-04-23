using System.ComponentModel.DataAnnotations;

namespace MangoTaika.DTOs;

public class GroupeDto
{
    public Guid Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Adresse { get; set; }
    public string? NomChefGroupe { get; set; }
    public Guid? ChefGroupeScoutId { get; set; }
    public Guid? ResponsableId { get; set; }
    public string? ContactChefGroupe { get; set; }
    public string? ResponsablePhotoUrl { get; set; }
    public int NombreMembres { get; set; }
    public int NombreFilles { get; set; }
    public int NombreGarcons { get; set; }
    public RepartitionMembresDto Jeunes { get; set; } = new();
    public RepartitionMembresDto Adultes { get; set; } = new();
    public List<BrancheScoutCountDto> BranchesScouts { get; set; } = [];
    public List<GroupeMembreDto> Membres { get; set; } = [];
}

public class BrancheScoutCountDto
{
    public string Nom { get; set; } = string.Empty;
    public int NombreScouts { get; set; }
    public int NombreFilles { get; set; }
    public int NombreGarcons { get; set; }
    public string? NomChefUnite { get; set; }
    public RepartitionMembresDto Jeunes { get; set; } = new();
    public RepartitionMembresDto Adultes { get; set; } = new();
}

public class GroupeMembreDto
{
    public string Matricule { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public string Prenoms { get; set; } = string.Empty;
    public string? Branche { get; set; }
    public string Fonction { get; set; } = "-";
    public string TypeMembre { get; set; } = "Jeune";
}

public class RepartitionMembresDto
{
    public int NombreFeminin { get; set; }
    public int NombreMasculin { get; set; }
    public int NombreNonRenseigne { get; set; }
    public int Total => NombreFeminin + NombreMasculin + NombreNonRenseigne;
}

public class GroupeCreateDto : IValidatableObject
{
    [Required(ErrorMessage = "Le nom du groupe est obligatoire.")]
    public string Nom { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Commune { get; set; }
    public string? Quartier { get; set; }
    public string? NomChefGroupe { get; set; }
    public Guid? ChefGroupeScoutId { get; set; }
    public string? LogoUrl { get; set; }
    public Guid? ResponsableId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Latitude.HasValue && (Latitude.Value < -90 || Latitude.Value > 90))
        {
            yield return new ValidationResult(
                "La latitude doit etre comprise entre -90 et 90.",
                [nameof(Latitude)]);
        }

        if (Longitude.HasValue && (Longitude.Value < -180 || Longitude.Value > 180))
        {
            yield return new ValidationResult(
                "La longitude doit etre comprise entre -180 et 180.",
                [nameof(Longitude)]);
        }
    }
}
