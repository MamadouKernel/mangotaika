namespace MangoTaika.Data.Entities;

public class Competence
{
    public Guid Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DateObtention { get; set; }
    public string? Niveau { get; set; }
    public TypeCompetence Type { get; set; } = TypeCompetence.Scoute;

    // Navigation
    public Guid ScoutId { get; set; }
    public Scout Scout { get; set; } = null!;
}

public enum TypeCompetence
{
    Scoute,
    Academique,
    Autre
}
