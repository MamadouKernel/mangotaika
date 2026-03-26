namespace MangoTaika.Data.Entities;

public class QuestionQuiz
{
    public Guid Id { get; set; }
    public string Enonce { get; set; } = string.Empty;
    public int Ordre { get; set; }

    // Navigation
    public Guid QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;
    public ICollection<ReponseQuiz> Reponses { get; set; } = [];
}
