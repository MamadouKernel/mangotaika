namespace MangoTaika.Data.Entities;

public class ReponseQuiz
{
    public Guid Id { get; set; }
    public string Texte { get; set; } = string.Empty;
    public bool EstCorrecte { get; set; }
    public int Ordre { get; set; }

    // Navigation
    public Guid QuestionId { get; set; }
    public QuestionQuiz Question { get; set; } = null!;
}
