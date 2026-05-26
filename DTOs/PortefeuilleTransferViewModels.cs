using System.ComponentModel.DataAnnotations;

namespace MangoTaika.DTOs;

public class TransferCandidateDto
{
    public Guid Id { get; set; }
    public string NomComplet { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telephone { get; set; }
    public string? Matricule { get; set; }
}

public class ConfirmTransferViewModel
{
    [Required]
    public Guid BeneficiaireId { get; set; }

    [Range(1, double.MaxValue)]
    public decimal Montant { get; set; }

    public string Devise { get; set; } = "XOF";
    public decimal SoldeActuel { get; set; }
    public decimal SoldeApres => SoldeActuel - Montant;
    public decimal MontantMinimum { get; set; }
    public decimal MontantMaximum { get; set; }
    public decimal PlafondJournalierRestant { get; set; }
    public string? Commentaire { get; set; }
    public List<TransferCandidateDto> Candidats { get; set; } = [];
}
