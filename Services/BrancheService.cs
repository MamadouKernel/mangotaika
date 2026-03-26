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
        var branche = new Branche
        {
            Id = Guid.NewGuid(),
            Nom = dto.Nom,
            Description = dto.Description,
            AgeMin = dto.AgeMin,
            AgeMax = dto.AgeMax,
            ChefUniteId = dto.ChefUniteId,
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
        branche.Nom = dto.Nom;
        branche.Description = dto.Description;
        branche.AgeMin = dto.AgeMin;
        branche.AgeMax = dto.AgeMax;
        branche.ChefUniteId = dto.ChefUniteId;
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
}
