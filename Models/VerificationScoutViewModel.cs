namespace MangoTaika.Models;

public class VerificationScoutViewModel
{
    public string? Matricule { get; set; }
    public string? Nom { get; set; }
    public string? Prenom { get; set; }
    public bool RechercheEffectuee { get; set; }
    public string Statut { get; set; } = string.Empty;
    public string? NomComplet { get; set; }
    public string? MatriculeTrouve { get; set; }
    public string? Groupe { get; set; }
    public string? Branche { get; set; }
}
