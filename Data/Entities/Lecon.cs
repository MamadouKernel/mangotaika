namespace MangoTaika.Data.Entities;

public class Lecon
{
    public Guid Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public TypeLecon Type { get; set; } = TypeLecon.Texte;
    public string? ContenuTexte { get; set; }
    public string? VideoUrl { get; set; }
    public string? DocumentUrl { get; set; }
    public int Ordre { get; set; }
    public int DureeMinutes { get; set; }

    // Navigation
    public Guid ModuleId { get; set; }
    public ModuleFormation Module { get; set; } = null!;
    public ICollection<ProgressionLecon> Progressions { get; set; } = [];
}

public enum TypeLecon { Texte, Video, Document }
