using MangoTaika.Data.Entities;

namespace MangoTaika.DTOs;

public class DemandeGroupeDto
{
    public Guid Id { get; set; }
    public string NomGroupe { get; set; } = string.Empty;
    public string Commune { get; set; } = string.Empty;
    public string Quartier { get; set; } = string.Empty;
    public string NomResponsable { get; set; } = string.Empty;
    public string TelephoneResponsable { get; set; } = string.Empty;
    public string? EmailResponsable { get; set; }
    public string? Motivation { get; set; }
    public int NombreMembresPrevus { get; set; }
    public StatutDemandeGroupe Statut { get; set; }
    public string? MotifRejet { get; set; }
    public DateTime DateCreation { get; set; }
}

public class DemandeGroupeCreateDto
{
    public string NomGroupe { get; set; } = string.Empty;
    public string Commune { get; set; } = string.Empty;
    public string Quartier { get; set; } = string.Empty;
    public string NomResponsable { get; set; } = string.Empty;
    public string TelephoneResponsable { get; set; } = string.Empty;
    public string? EmailResponsable { get; set; }
    public string? Motivation { get; set; }
    public int NombreMembresPrevus { get; set; }
}
