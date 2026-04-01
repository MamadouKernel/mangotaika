using MangoTaika.Data.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MangoTaika.DTOs;

public class CotisationsNationalesIndexViewModel
{
    public int AnneeReference { get; set; }
    public Guid? GroupeId { get; set; }
    public Guid? BrancheId { get; set; }
    public StatutLigneCotisationNationale? Statut { get; set; }
    public Guid? ImportId { get; set; }
    public CotisationNationaleImport? ImportSelectionne { get; set; }
    public List<CotisationNationaleImportLigne> Lignes { get; set; } = [];
    public List<SelectListItem> Groupes { get; set; } = [];
    public List<SelectListItem> Branches { get; set; } = [];
    public List<SelectListItem> Imports { get; set; } = [];
    public int NombreVisible { get; set; }
    public int NombreAjour { get; set; }
    public int NombreNonAjour { get; set; }
    public int NombreAVerifier { get; set; }
}
