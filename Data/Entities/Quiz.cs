namespace MangoTaika.Data.Entities;

public class Quiz
{
    public Guid Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public int NoteMinimale { get; set; } = 70;
    public int? NombreTentativesMax { get; set; }
    public DateTime? DateOuvertureDisponibilite { get; set; }
    public DateTime? DateFermetureDisponibilite { get; set; }

    public Guid ModuleId { get; set; }
    public ModuleFormation Module { get; set; } = null!;
    public ICollection<QuestionQuiz> Questions { get; set; } = [];
    public ICollection<TentativeQuiz> Tentatives { get; set; } = [];
}
