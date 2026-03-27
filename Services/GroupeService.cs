using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Services;

public class GroupeService(AppDbContext db, IGeocodingService geocoding) : IGroupeService
{
    public async Task<List<GroupeDto>> GetAllAsync()
    {
        return await db.Groupes
            .Include(g => g.Responsable)
            .Where(g => g.IsActive)
            .Select(g => new GroupeDto
            {
                Id = g.Id,
                Nom = g.Nom,
                Description = g.Description,
                Latitude = g.Latitude,
                Longitude = g.Longitude,
                Adresse = g.Adresse,
                NomChefGroupe = g.NomChefGroupe != null && g.NomChefGroupe != string.Empty
                    ? g.NomChefGroupe
                    : (g.Responsable != null ? g.Responsable.Prenom + " " + g.Responsable.Nom : null),
                NombreMembres = db.Scouts.Count(s => s.GroupeId == g.Id && s.IsActive),
                BranchesScouts = db.Branches
                    .Where(b => b.GroupeId == g.Id && b.IsActive)
                    .Select(b => new BrancheScoutCountDto
                    {
                        Nom = b.Nom,
                        NombreScouts = db.Scouts.Count(s => s.BrancheId == b.Id && s.IsActive),
                        NomChefUnite = b.NomChefUnite
                    }).ToList()
            })
            .ToListAsync();
    }

    public async Task<GroupeDto?> GetByIdAsync(Guid id)
    {
        var groupe = await db.Groupes
            .Include(g => g.Responsable)
            .FirstOrDefaultAsync(g => g.Id == id);
        if (groupe is null) return null;
        return new GroupeDto
        {
            Id = groupe.Id,
            Nom = groupe.Nom,
            Description = groupe.Description,
            Latitude = groupe.Latitude,
            Longitude = groupe.Longitude,
            Adresse = groupe.Adresse,
            NomChefGroupe = BuildChefGroupeName(
                groupe.NomChefGroupe,
                groupe.Responsable != null ? $"{groupe.Responsable.Prenom} {groupe.Responsable.Nom}" : null),
            NombreMembres = await db.Scouts.CountAsync(s => s.GroupeId == groupe.Id && s.IsActive),
            BranchesScouts = await db.Branches
                .Where(b => b.GroupeId == groupe.Id && b.IsActive)
                .Select(b => new BrancheScoutCountDto
                {
                    Nom = b.Nom,
                    NombreScouts = db.Scouts.Count(s => s.BrancheId == b.Id && s.IsActive),
                    NomChefUnite = b.NomChefUnite
                }).ToListAsync()
        };
    }

    public async Task<GroupeDto> CreateAsync(GroupeCreateDto dto)
    {
        var adresse = BuildAdresse(dto.Commune, dto.Quartier);
        var (lat, lng) = await geocoding.GeocodeAsync(adresse ?? "");

        var groupe = new Groupe
        {
            Id = Guid.NewGuid(),
            Nom = dto.Nom,
            Description = NormalizeOptional(dto.Description),
            Latitude = lat ?? dto.Latitude,
            Longitude = lng ?? dto.Longitude,
            Adresse = adresse,
            NomChefGroupe = NormalizeOptional(dto.NomChefGroupe),
            ResponsableId = dto.ResponsableId
        };
        db.Groupes.Add(groupe);
        await db.SaveChangesAsync();
        return ToDto(groupe);
    }

    public async Task<bool> UpdateAsync(Guid id, GroupeCreateDto dto)
    {
        var groupe = await db.Groupes.FindAsync(id);
        if (groupe is null) return false;

        var adresse = BuildAdresse(dto.Commune, dto.Quartier);
        var (lat, lng) = await geocoding.GeocodeAsync(adresse ?? "");

        groupe.Nom = dto.Nom;
        groupe.Description = NormalizeOptional(dto.Description);
        groupe.Latitude = lat ?? dto.Latitude;
        groupe.Longitude = lng ?? dto.Longitude;
        groupe.Adresse = adresse;
        groupe.NomChefGroupe = NormalizeOptional(dto.NomChefGroupe);
        groupe.ResponsableId = dto.ResponsableId;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var groupe = await db.Groupes.FindAsync(id);
        if (groupe is null) return false;
        groupe.IsActive = false;
        await db.SaveChangesAsync();
        return true;
    }

    private static GroupeDto ToDto(Groupe g) => new()
    {
        Id = g.Id,
        Nom = g.Nom,
        Description = g.Description,
        Latitude = g.Latitude,
        Longitude = g.Longitude,
        Adresse = g.Adresse,
        NomChefGroupe = BuildChefGroupeName(
            g.NomChefGroupe,
            g.Responsable != null ? $"{g.Responsable.Prenom} {g.Responsable.Nom}" : null),
        NombreMembres = 0
    };

    private static string? BuildAdresse(string? commune, string? quartier)
    {
        var parts = new[] { NormalizeOptional(quartier), NormalizeOptional(commune) }
            .Where(p => p is not null);
        var result = string.Join(", ", parts);
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    private static string? BuildChefGroupeName(string? storedName, string? fallbackName)
    {
        return NormalizeOptional(storedName) ?? NormalizeOptional(fallbackName);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
