using System.ComponentModel.DataAnnotations;
using MangoTaika.Data.Entities;

namespace MangoTaika.DTOs;

public class DemandeAutorisationDto
{
    public Guid Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TypeActiviteDemande TypeActivite { get; set; }
    public DateTime DateActivite { get; set; }
    public DateTime? DateFin { get; set; }
    public string? Lieu { get; set; }
    public int NombreParticipants { get; set; }
    public string? Objectifs { get; set; }
    public string? Responsables { get; set; }
    public string? MoyensLogistiques { get; set; }
    public string? Budget { get; set; }
    public string? Observations { get; set; }
    public string? TdrContenu { get; set; }
    public StatutDemande Statut { get; set; }
    public string? MotifRejet { get; set; }
    public DateTime DateCreation { get; set; }
    public DateTime? DateValidation { get; set; }
    public string? NomDemandeur { get; set; }
    public string? NomValideur { get; set; }
    public string? NomGroupe { get; set; }
    public Guid? GroupeId { get; set; }
    public string? NomBranche { get; set; }
    public Guid? BrancheId { get; set; }
    public List<SuiviDemandeDto> Suivis { get; set; } = [];
}

public class DemandeAutorisationCreateDto : IValidatableObject
{
    [Required(ErrorMessage = "Le nom de l'activite est obligatoire.")]
    [Display(Name = "Nom de l'activite")]
    public string Titre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La description est obligatoire.")]
    public string? Description { get; set; }

    [Display(Name = "Type d'activite")]
    public TypeActiviteDemande TypeActivite { get; set; }

    [Required(ErrorMessage = "La date de debut est obligatoire.")]
    [Display(Name = "Date de debut")]
    public DateTime DateActivite { get; set; }

    [Display(Name = "Date de fin")]
    public DateTime? DateFin { get; set; }

    [Required(ErrorMessage = "Le lieu est obligatoire.")]
    public string? Lieu { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Le nombre de participants doit etre superieur a zero.")]
    [Display(Name = "Nombre de participants")]
    public int NombreParticipants { get; set; }

    [Required(ErrorMessage = "L'objectif de l'activite est obligatoire.")]
    public string? Objectifs { get; set; }

    [Required(ErrorMessage = "Le ou les responsables doivent etre renseignes.")]
    public string? Responsables { get; set; }

    [Display(Name = "Moyens logistiques")]
    public string? MoyensLogistiques { get; set; }

    [Display(Name = "Budget previsionnel")]
    public string? Budget { get; set; }

    public string? Observations { get; set; }

    [Required(ErrorMessage = "Le groupe concerne est obligatoire.")]
    [Display(Name = "Groupe concerne")]
    public Guid? GroupeId { get; set; }

    [Required(ErrorMessage = "La branche concernee est obligatoire.")]
    [Display(Name = "Branche concernee")]
    public Guid? BrancheId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateFin.HasValue && DateFin.Value.Date < DateActivite.Date)
        {
            yield return new ValidationResult(
                "La date de fin doit etre posterieure ou egale a la date de debut.",
                [nameof(DateFin)]);
        }
    }
}

public class SuiviDemandeDto
{
    public StatutDemande AncienStatut { get; set; }
    public StatutDemande NouveauStatut { get; set; }
    public string? Commentaire { get; set; }
    public string? Auteur { get; set; }
    public DateTime Date { get; set; }
}
