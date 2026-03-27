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
        var nom = NormalizeNom(dto.Nom);
        ValidateCoordinates(dto.Latitude, dto.Longitude);
        await EnsureUniqueNomAsync(nom);
        await EnsureResponsableExistsAsync(dto.ResponsableId);
        var adresse = BuildAdresse(dto.Commune, dto.Quartier);
        var (lat, lng) = await geocoding.GeocodeAsync(adresse ?? "");

        var groupe = new Groupe
        {
            Id = Guid.NewGuid(),
            Nom = nom,
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

        var nom = NormalizeNom(dto.Nom);
        ValidateCoordinates(dto.Latitude, dto.Longitude);
        await EnsureUniqueNomAsync(nom, id);
        await EnsureResponsableExistsAsync(dto.ResponsableId);
        var adresse = BuildAdresse(dto.Commune, dto.Quartier);
        var (lat, lng) = await geocoding.GeocodeAsync(adresse ?? "");

        groupe.Nom = nom;
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

    private static string NormalizeNom(string nom)
    {
        var normalized = nom?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Le nom du groupe est obligatoire.");
        }

        return normalized;
    }

    private static void ValidateCoordinates(double? latitude, double? longitude)
    {
        if (latitude.HasValue && (latitude.Value < -90 || latitude.Value > 90))
        {
            throw new InvalidOperationException("La latitude doit etre comprise entre -90 et 90.");
        }

        if (longitude.HasValue && (longitude.Value < -180 || longitude.Value > 180))
        {
            throw new InvalidOperationException("La longitude doit etre comprise entre -180 et 180.");
        }
    }

    private async Task EnsureUniqueNomAsync(string nom, Guid? currentId = null)
    {
        var exists = await db.Groupes.AnyAsync(g =>
            g.IsActive &&
            g.Id != currentId &&
            g.Nom.ToUpper() == nom.ToUpper());

        if (exists)
        {
            throw new InvalidOperationException("Un groupe avec ce nom existe deja.");
        }
    }

    private async Task EnsureResponsableExistsAsync(Guid? responsableId)
    {
        if (!responsableId.HasValue)
        {
            return;
        }

        var exists = await db.Users.AnyAsync(u => u.Id == responsableId.Value && u.IsActive);
        if (!exists)
        {
            throw new InvalidOperationException("Le responsable selectionne est introuvable ou inactif.");
        }
    }
}
