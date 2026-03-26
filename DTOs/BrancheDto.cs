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

public class BrancheCreateDto
{
    public string Nom { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? AgeMin { get; set; }
    public int? AgeMax { get; set; }
    public Guid? ChefUniteId { get; set; }
    public Guid GroupeId { get; set; }
}
