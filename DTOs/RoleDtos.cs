using System.ComponentModel.DataAnnotations;

namespace MangoTaika.DTOs;

public class RoleViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Libelle { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Visibilite { get; set; }
    public int Hierarchie { get; set; }
    public bool EstSysteme { get; set; }
}

public class RoleEditDto
{
    [Required(ErrorMessage = "Le nom technique du role est obligatoire.")]
    [Display(Name = "Nom technique")]
    [RegularExpression("^[A-Za-z0-9_]+$", ErrorMessage = "Le nom technique ne doit contenir que des lettres, chiffres ou underscores.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le libelle du role est obligatoire.")]
    [StringLength(120)]
    [Display(Name = "Libelle")]
    public string? Libelle { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(200)]
    [Display(Name = "Perimetre / visibilite")]
    public string? Visibilite { get; set; }

    [Range(0, 99)]
    [Display(Name = "Niveau hierarchique")]
    public int Hierarchie { get; set; } = 50;
}
