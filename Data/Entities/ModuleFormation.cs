namespace MangoTaika.Data.Entities;

public class ModuleFormation
{
    public Guid Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Ordre { get; set; }

    // Navigation
    public Guid FormationId { get; set; }
    public Formation Formation { get; set; } = null!;
    public ICollection<Lecon> Lecons { get; set; } = [];
    public Quiz? Quiz { get; set; }
}
