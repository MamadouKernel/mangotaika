namespace MangoTaika.DTOs;

public class GroupeDto
{
    public Guid Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Adresse { get; set; }
    public string? NomResponsable { get; set; }
    public string? NomAdjoints { get; set; }
    public int NombreMembres { get; set; }
    public List<BrancheScoutCountDto> BranchesScouts { get; set; } = [];
}

public class BrancheScoutCountDto
{
    public string Nom { get; set; } = string.Empty;
    public int NombreScouts { get; set; }
    public string? NomChefUnite { get; set; }
}

public class GroupeCreateDto
{
    public string Nom { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Commune { get; set; }
    public string? Quartier { get; set; }
    public string? NomAdjoints { get; set; }
    public Guid? ResponsableId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
