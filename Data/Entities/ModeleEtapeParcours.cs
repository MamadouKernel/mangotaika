using System.ComponentModel.DataAnnotations;

namespace MangoTaika.Data.Entities;

public class ModeleEtapeParcours
{
    public Guid Id { get; set; }
    public Guid? BrancheId { get; set; }
    public Branche? Branche { get; set; }

    [Required]
    [StringLength(180)]
    public string NomEtape { get; set; } = string.Empty;

    [StringLength(100)]
    public string? CodeEtape { get; set; }

    public int OrdreAffichage { get; set; }
    public bool EstObligatoire { get; set; } = true;
    public bool IsActive { get; set; } = true;

    [StringLength(2000)]
    public string? Description { get; set; }
}
