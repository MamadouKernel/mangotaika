namespace MangoTaika.Data.Entities;

public class DiscussionFormation
{
    public Guid Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string ContenuInitial { get; set; } = string.Empty;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime DateDerniereActivite { get; set; } = DateTime.UtcNow;
    public bool EstVerrouillee { get; set; }

    public Guid FormationId { get; set; }
    public Formation Formation { get; set; } = null!;

    public Guid AuteurId { get; set; }
    public ApplicationUser Auteur { get; set; } = null!;

    public ICollection<MessageDiscussionFormation> Messages { get; set; } = [];
}
