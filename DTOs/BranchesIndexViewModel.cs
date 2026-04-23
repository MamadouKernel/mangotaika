using Microsoft.AspNetCore.Mvc.Rendering;

namespace MangoTaika.DTOs;

public class BranchesIndexViewModel
{
    public List<BrancheDto> Branches { get; set; } = [];
    public Guid? EntiteId { get; set; }
    public string? NomBranche { get; set; }
    public List<SelectListItem> Entites { get; set; } = [];
    public List<SelectListItem> NomsBranches { get; set; } = [];
}
