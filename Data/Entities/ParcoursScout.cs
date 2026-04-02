using System.ComponentModel.DataAnnotations;

namespace MangoTaika.Data.Entities;

public class EtapeParcoursScout
{
    public Guid Id { get; set; }

    public Guid ScoutId { get; set; }
    public Scout Scout { get; set; } = null!;

    [Required]
    [StringLength(180)]
    public string NomEtape { get; set; } = string.Empty;

    [StringLength(100)]
    public string? CodeEtape { get; set; }

    [Range(1, int.MaxValue)]
    public int OrdreAffichage { get; set; } = 1;

    public DateTime? DateValidation { get; set; }
    public DateTime? DatePrevisionnelle { get; set; }

    [StringLength(2000)]
    public string? Observations { get; set; }

    public bool EstObligatoire { get; set; } = true;
}
