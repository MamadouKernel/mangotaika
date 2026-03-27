using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Services;

public class BrancheService(AppDbContext db) : IBrancheService
{
    public async Task<List<BrancheDto>> GetAllAsync()
    {
        return await db.Branches
            .Include(b => b.Groupe)
            .Include(b => b.Scouts)
            .Include(b => b.ChefUnite)
            .Where(b => b.IsActive)
            .Select(b => ToDto(b))
            .ToListAsync();
    }

    public async Task<BrancheDto?> GetByIdAsync(Guid id)
    {
        var branche = await db.Branches
            .Include(b => b.Groupe)
            .Include(b => b.Scouts)
            .Include(b => b.ChefUnite)
            .FirstOrDefaultAsync(b => b.Id == id);
        return branche is null ? null : ToDto(branche);
    }

    public async Task<BrancheDto> CreateAsync(BrancheCreateDto dto)
    {
        var chefUnite = await GetChefUniteAsync(dto.GroupeId, dto.ChefUniteId);

        var branche = new Branche
        {
            Id = Guid.NewGuid(),
            Nom = dto.Nom,
            Description = dto.Description,
            AgeMin = dto.AgeMin,
            AgeMax = dto.AgeMax,
            ChefUniteId = chefUnite.Id,
            NomChefUnite = $"{chefUnite.Prenom} {chefUnite.Nom}",
            GroupeId = dto.GroupeId
        };
        db.Branches.Add(branche);
        await db.SaveChangesAsync();
        return ToDto(branche);
    }

    public async Task<bool> UpdateAsync(Guid id, BrancheCreateDto dto)
    {
        var branche = await db.Branches.FindAsync(id);
        if (branche is null) return false;
        var chefUnite = await GetChefUniteAsync(dto.GroupeId, dto.ChefUniteId);
        branche.Nom = dto.Nom;
        branche.Description = dto.Description;
        branche.AgeMin = dto.AgeMin;
        branche.AgeMax = dto.AgeMax;
        branche.ChefUniteId = chefUnite.Id;
        branche.NomChefUnite = $"{chefUnite.Prenom} {chefUnite.Nom}";
        branche.GroupeId = dto.GroupeId;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var branche = await db.Branches.FindAsync(id);
        if (branche is null) return false;
        branche.IsActive = false;
        await db.SaveChangesAsync();
        return true;
    }

    private static BrancheDto ToDto(Branche b) => new()
    {
        Id = b.Id,
        Nom = b.Nom,
        Description = b.Description,
        AgeMin = b.AgeMin,
        AgeMax = b.AgeMax,
        NomChefUnite = b.ChefUnite != null ? $"{b.ChefUnite.Prenom} {b.ChefUnite.Nom}" : b.NomChefUnite,
        ChefUniteId = b.ChefUniteId,
        GroupeId = b.GroupeId,
        NomGroupe = b.Groupe?.Nom,
        NombreScouts = b.Scouts.Count
    };

    private async Task<Scout> GetChefUniteAsync(Guid groupeId, Guid? chefUniteId)
    {
        if (!chefUniteId.HasValue)
        {
            throw new InvalidOperationException("Le chef d'unité est obligatoire.");
        }

        var chefUnite = await db.Scouts
            .FirstOrDefaultAsync(s => s.Id == chefUniteId.Value && s.IsActive);

        if (chefUnite is null)
        {
            throw new InvalidOperationException("Le chef d'unité sélectionné est introuvable.");
        }

        if (chefUnite.GroupeId != groupeId)
        {
            throw new InvalidOperationException("Le chef d'unité doit appartenir au groupe sélectionné.");
        }

        return chefUnite;
    }
}
