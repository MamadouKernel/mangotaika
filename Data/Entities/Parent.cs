namespace MangoTaika.Data.Entities;

public class Parent
{
    public Guid Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    public Guid? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public string? Relation { get; set; } // Pere, Mere, Tuteur

    // Navigation
    public ICollection<Scout> Scouts { get; set; } = [];
}
