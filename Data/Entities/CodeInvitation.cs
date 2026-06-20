namespace MangoTaika.Data.Entities;

public class CodeInvitation
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string RoleCible { get; set; } = "Gestionnaire";
    public bool EstActif { get; set; } = true;
    public bool EstUtilise { get; set; } = false;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime? DateExpiration { get; set; }
    public DateTime? DateUtilisation { get; set; }

    // Qui a créé le code (admin)
    public Guid CreateurId { get; set; }
    public ApplicationUser Createur { get; set; } = null!;

    // Qui a utilisé le code (gestionnaire)
    public Guid? UtilisePaId { get; set; }
    public ApplicationUser? UtilisePar { get; set; }
}
