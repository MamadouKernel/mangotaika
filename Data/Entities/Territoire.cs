namespace MangoTaika.Data.Entities;

public class RegionScoute
{
    public Guid Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool EstActive { get; set; } = true;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public ICollection<DistrictScout> Districts { get; set; } = [];
}

public class DistrictScout
{
    public Guid Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? RegionScouteId { get; set; }
    public RegionScoute? RegionScoute { get; set; }
    public bool EstActif { get; set; } = true;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public ICollection<Groupe> Groupes { get; set; } = [];
}
