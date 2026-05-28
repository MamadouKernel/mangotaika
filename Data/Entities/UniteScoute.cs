using System.ComponentModel.DataAnnotations;

namespace MangoTaika.Data.Entities;

public class UniteScoute
{
    public Guid Id { get; set; }
    public Guid GroupeId { get; set; }
    public Groupe Groupe { get; set; } = null!;
    public Guid BrancheId { get; set; }
    public Branche Branche { get; set; } = null!;
    [Required]
    [StringLength(160)]
    public string Nom { get; set; } = string.Empty;
    [StringLength(1200)]
    public string? Description { get; set; }
    [StringLength(1200)]
    public string? Attributs { get; set; }
    [StringLength(500)]
    public string? ImageUrl { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public Guid? CreateurId { get; set; }
    public ApplicationUser? Createur { get; set; }
    public bool EstActive { get; set; } = true;
    public bool EstSupprime { get; set; }
    public ICollection<RoleUniteScoute> Roles { get; set; } = [];
    public ICollection<AffectationUniteScoute> Affectations { get; set; } = [];
}

public class RoleUniteScoute
{
    public Guid Id { get; set; }
    public Guid UniteScouteId { get; set; }
    public UniteScoute UniteScoute { get; set; } = null!;
    [Required]
    [StringLength(120)]
    public string Nom { get; set; } = string.Empty;
    [StringLength(600)]
    public string? Description { get; set; }
    public bool EstSupprime { get; set; }
}

public class AffectationUniteScoute
{
    public Guid Id { get; set; }
    public Guid UniteScouteId { get; set; }
    public UniteScoute UniteScoute { get; set; } = null!;
    public Guid ScoutId { get; set; }
    public Scout Scout { get; set; } = null!;
    public Guid? RoleUniteScouteId { get; set; }
    public RoleUniteScoute? RoleUniteScoute { get; set; }
    public DateTime DateAffectation { get; set; } = DateTime.UtcNow;
    public bool EstActif { get; set; } = true;
}
