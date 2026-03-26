namespace MangoTaika.Data.Entities;

public class SuiviAcademique
{
    public Guid Id { get; set; }
    public string AnneeScolaire { get; set; } = string.Empty; // Ex: "2025-2026"
    public string? Etablissement { get; set; }
    public string NiveauScolaire { get; set; } = string.Empty; // Ex: "CM2", "6ème", "Terminale"
    public string? Classe { get; set; }
    public double? MoyenneGenerale { get; set; }
    public string? Mention { get; set; } // Ex: "Bien", "Assez Bien", "Passable"
    public string? Observations { get; set; }
    public bool EstRedoublant { get; set; } = false;
    public DateTime DateSaisie { get; set; } = DateTime.UtcNow;

    // Navigation
    public Guid ScoutId { get; set; }
    public Scout Scout { get; set; } = null!;
}
