using MangoTaika.Data.Entities;

namespace MangoTaika.DTOs;

public class ScoutProgressionViewModel
{
    public Scout Scout { get; set; } = null!;
    public List<ScoutParcoursEtapeViewModel> Etapes { get; set; } = [];
    public List<ScoutParcoursEtapeViewModel> EtapesValidees { get; set; } = [];
    public List<ScoutParcoursEtapeViewModel> EtapesRestantes { get; set; } = [];
    public ScoutParcoursEtapeViewModel? ProchaineEtape { get; set; }
    public List<ParticipantActivite> ActivitesParticipees { get; set; } = [];
    public List<ModeleEtapeParcours> ReferentielEtapes { get; set; } = [];
    public int ProchainePosition { get; set; }
    public bool AUnReferentiel => ReferentielEtapes.Count > 0;
}

public class ScoutParcoursEtapeViewModel
{
    public Guid ScoutId { get; set; }
    public Guid? EtapeParcoursId { get; set; }
    public Guid? ModeleEtapeParcoursId { get; set; }
    public string NomEtape { get; set; } = string.Empty;
    public string? CodeEtape { get; set; }
    public int OrdreAffichage { get; set; }
    public DateTime? DateValidation { get; set; }
    public DateTime? DatePrevisionnelle { get; set; }
    public string? Observations { get; set; }
    public bool EstObligatoire { get; set; } = true;
    public bool EstIssueReferentiel { get; set; }
    public bool ExistePourScout { get; set; }
    public bool EstPersonnalisee => !EstIssueReferentiel;
}

public class EnregistrerEtapeParcoursDto
{
    public Guid ScoutId { get; set; }
    public Guid? EtapeParcoursId { get; set; }
    public Guid? ModeleEtapeParcoursId { get; set; }
    public string? NomEtape { get; set; }
    public string? CodeEtape { get; set; }
    public int OrdreAffichage { get; set; }
    public DateTime? DateValidation { get; set; }
    public DateTime? DatePrevisionnelle { get; set; }
    public string? Observations { get; set; }
    public bool EstObligatoire { get; set; } = true;
}

public class ModeleEtapeParcoursCreateDto
{
    public Guid ScoutId { get; set; }
    public Guid? BrancheId { get; set; }
    public string NomEtape { get; set; } = string.Empty;
    public string? CodeEtape { get; set; }
    public int OrdreAffichage { get; set; }
    public bool EstObligatoire { get; set; } = true;
    public string? Description { get; set; }
}
