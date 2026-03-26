namespace MangoTaika.Data.Entities;

public class Parent
{
    public Guid Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    public string? Relation { get; set; } // Père, Mère, Tuteur

    // Navigation
    public ICollection<Scout> Scouts { get; set; } = [];
}
