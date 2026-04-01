namespace MangoTaika.Data.Entities;

public enum StatutLigneCotisationNationale
{
    Ajour,
    NonAjour,
    AVerifier
}

public class CotisationNationaleImport
{
    public Guid Id { get; set; }
    public int AnneeReference { get; set; }
    public string NomFichier { get; set; } = string.Empty;
    public DateTime DateImport { get; set; } = DateTime.UtcNow;
    public decimal MontantTotal { get; set; }
    public int NombreAjour { get; set; }
    public int NombreNonAjour { get; set; }
    public int NombreAVerifier { get; set; }
    public Guid CreateurId { get; set; }
    public ApplicationUser Createur { get; set; } = null!;
    public ICollection<CotisationNationaleImportLigne> Lignes { get; set; } = [];
}

public class CotisationNationaleImportLigne
{
    public Guid Id { get; set; }
    public Guid ImportId { get; set; }
    public CotisationNationaleImport Import { get; set; } = null!;
    public Guid? ScoutId { get; set; }
    public Scout? Scout { get; set; }
    public string Matricule { get; set; } = string.Empty;
    public string? NomImporte { get; set; }
    public decimal? Montant { get; set; }
    public StatutLigneCotisationNationale Statut { get; set; }
    public string? Motif { get; set; }
}
