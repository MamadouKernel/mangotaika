namespace MangoTaika.Data.Entities;

public class TentativeQuiz
{
    public Guid Id { get; set; }
    public int Score { get; set; } // % obtenu
    public bool Reussi { get; set; }
    public DateTime DateTentative { get; set; } = DateTime.UtcNow;
    public string? ReponsesJson { get; set; } // Sérialisation des réponses données

    // Navigation
    public Guid ScoutId { get; set; }
    public Scout Scout { get; set; } = null!;
    public Guid QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;
}
