namespace MangoTaika.Data.Entities;

public class FormationPrerequis
{
    public Guid FormationId { get; set; }
    public Formation Formation { get; set; } = null!;

    public Guid PrerequisFormationId { get; set; }
    public Formation PrerequisFormation { get; set; } = null!;
}
