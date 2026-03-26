namespace MangoTaika.Data.Entities;

public class ProgressionLecon
{
    public Guid Id { get; set; }
    public bool EstTerminee { get; set; }
    public DateTime? DateTerminee { get; set; }

    // Navigation
    public Guid ScoutId { get; set; }
    public Scout Scout { get; set; } = null!;
    public Guid LeconId { get; set; }
    public Lecon Lecon { get; set; } = null!;
}
