namespace MangoTaika.Data.Entities;

public class JalonFormation
{
    public Guid Id { get; set; }
    public Guid FormationId { get; set; }
    public Formation Formation { get; set; } = null!;
    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DateJalon { get; set; }
    public TypeJalonFormation Type { get; set; } = TypeJalonFormation.Rappel;
    public bool EstPublie { get; set; } = true;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
}

public enum TypeJalonFormation
{
    Demarrage,
    ClasseVirtuelle,
    Remise,
    Evaluation,
    Rappel
}
