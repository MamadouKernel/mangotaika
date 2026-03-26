namespace MangoTaika.Data.Entities;

public class MessageDiscussionFormation
{
    public Guid Id { get; set; }
    public string Contenu { get; set; } = string.Empty;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public bool EstSupprime { get; set; }

    public Guid DiscussionFormationId { get; set; }
    public DiscussionFormation Discussion { get; set; } = null!;

    public Guid AuteurId { get; set; }
    public ApplicationUser Auteur { get; set; } = null!;
}
