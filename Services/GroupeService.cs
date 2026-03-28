using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
using MangoTaika.Helpers;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Services;

public class GroupeService(AppDbContext db, IGeocodingService geocoding, DistrictBranchInheritanceService districtBranchInheritance) : IGroupeService
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
                LogoUrl = g.LogoUrl,
                Latitude = g.Latitude,
                Longitude = g.Longitude,
                Adresse = g.Adresse,
                NomChefGroupe = g.NomChefGroupe != null && g.NomChefGroupe != string.Empty
                    ? g.NomChefGroupe
                    : (g.Responsable != null ? g.Responsable.Prenom + " " + g.Responsable.Nom : null),
                ResponsableId = g.ResponsableId,
                ContactChefGroupe = NormalizeOptional(g.Responsable != null ? g.Responsable.PhoneNumber : null),
                ResponsablePhotoUrl = NormalizeOptional(g.Responsable != null ? g.Responsable.PhotoUrl : null),
                NombreMembres = db.Scouts.Count(s => s.GroupeId == g.Id && s.IsActive),
                BranchesScouts = db.Branches
                    .Where(b => b.GroupeId == g.Id && b.IsActive)
                    .Select(b => new BrancheScoutCountDto
                    {
                        Nom = b.Nom,
                        NombreScouts = db.Scouts.Count(s => s.BrancheId == b.Id && s.IsActive),
                        NombreFilles = db.Scouts.Count(s => s.BrancheId == b.Id && s.IsActive && (
                            s.Sexe == "F" || s.Sexe == "Feminin" || s.Sexe == "Fille")),
                        NombreGarcons = db.Scouts.Count(s => s.BrancheId == b.Id && s.IsActive && (
                            s.Sexe == "M" || s.Sexe == "Masculin" || s.Sexe == "Garcon")),
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

        var scouts = await db.Scouts
            .Where(s => s.GroupeId == groupe.Id && s.IsActive)
            .ToListAsync();

        var branches = await db.Branches
            .Where(b => b.GroupeId == groupe.Id && b.IsActive)
            .OrderBy(b => b.Nom)
            .ToListAsync();

        return new GroupeDto
        {
            Id = groupe.Id,
            Nom = groupe.Nom,
            Description = groupe.Description,
            LogoUrl = groupe.LogoUrl,
            Latitude = groupe.Latitude,
            Longitude = groupe.Longitude,
            Adresse = groupe.Adresse,
            NomChefGroupe = BuildChefGroupeName(
                groupe.NomChefGroupe,
                groupe.Responsable != null ? $"{groupe.Responsable.Prenom} {groupe.Responsable.Nom}" : null),
            ResponsableId = groupe.ResponsableId,
            ContactChefGroupe = NormalizeOptional(groupe.Responsable?.PhoneNumber),
            ResponsablePhotoUrl = NormalizeOptional(groupe.Responsable?.PhotoUrl),
            NombreMembres = scouts.Count,
            NombreFilles = scouts.Count(s => ClassifySexe(s.Sexe) == SexeCategory.Feminin),
            NombreGarcons = scouts.Count(s => ClassifySexe(s.Sexe) == SexeCategory.Masculin),
            Jeunes = BuildRepartition(scouts.Where(IsJeune)),
            Adultes = BuildRepartition(scouts.Where(s => !IsJeune(s))),
            BranchesScouts = branches.Select(b =>
            {
                var branchScouts = scouts.Where(s => s.BrancheId == b.Id).ToList();
                return new BrancheScoutCountDto
                {
                    Nom = b.Nom,
                    NombreScouts = branchScouts.Count,
                    NombreFilles = branchScouts.Count(s => ClassifySexe(s.Sexe) == SexeCategory.Feminin),
                    NombreGarcons = branchScouts.Count(s => ClassifySexe(s.Sexe) == SexeCategory.Masculin),
                    NomChefUnite = b.NomChefUnite,
                    Jeunes = BuildRepartition(branchScouts.Where(IsJeune)),
                    Adultes = BuildRepartition(branchScouts.Where(s => !IsJeune(s)))
                };
            }).ToList()
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
            LogoUrl = NormalizeOptional(dto.LogoUrl),
            Latitude = lat ?? dto.Latitude,
            Longitude = lng ?? dto.Longitude,
            Adresse = adresse,
            NomChefGroupe = NormalizeOptional(dto.NomChefGroupe),
            ResponsableId = dto.ResponsableId
        };
        db.Groupes.Add(groupe);
        await SaveChangesAsync();
        await districtBranchInheritance.InheritDistrictBranchesForGroupAsync(groupe);
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
        groupe.LogoUrl = NormalizeOptional(dto.LogoUrl);
        groupe.Latitude = lat ?? dto.Latitude;
        groupe.Longitude = lng ?? dto.Longitude;
        groupe.Adresse = adresse;
        groupe.NomChefGroupe = NormalizeOptional(dto.NomChefGroupe);
        groupe.ResponsableId = dto.ResponsableId;
        await SaveChangesAsync();
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
        LogoUrl = g.LogoUrl,
        Latitude = g.Latitude,
        Longitude = g.Longitude,
        Adresse = g.Adresse,
        NomChefGroupe = BuildChefGroupeName(
            g.NomChefGroupe,
            g.Responsable != null ? $"{g.Responsable.Prenom} {g.Responsable.Nom}" : null),
        ResponsableId = g.ResponsableId,
        ContactChefGroupe = NormalizeOptional(g.Responsable?.PhoneNumber),
        ResponsablePhotoUrl = NormalizeOptional(g.Responsable?.PhotoUrl),
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

    private static RepartitionMembresDto BuildRepartition(IEnumerable<Scout> scouts)
    {
        var repartition = new RepartitionMembresDto();

        foreach (var scout in scouts)
        {
            switch (ClassifySexe(scout.Sexe))
            {
                case SexeCategory.Feminin:
                    repartition.NombreFeminin++;
                    break;
                case SexeCategory.Masculin:
                    repartition.NombreMasculin++;
                    break;
                default:
                    repartition.NombreNonRenseigne++;
                    break;
            }
        }

        return repartition;
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
        var normalizedNom = DatabaseText.NormalizeSearchKey(nom);
        var exists = db.Database.IsNpgsql()
            ? await db.Groupes.AnyAsync(g =>
                g.IsActive &&
                g.Id != currentId &&
                g.NomNormalise == normalizedNom)
            : (await db.Groupes
                .Where(g => g.IsActive && g.Id != currentId)
                .Select(g => g.Nom)
                .ToListAsync())
                .Any(existingNom => DatabaseText.NormalizeSearchKey(existingNom) == normalizedNom);

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

    private async Task SaveChangesAsync()
    {
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.ReferencesConstraint(PersistenceConstraints.GroupesNomNormaliseActif))
        {
            throw new InvalidOperationException("Un groupe avec ce nom existe deja.", ex);
        }
    }

    private static bool IsJeune(Scout scout)
    {
        var today = DateTime.UtcNow.Date;
        var birthDate = scout.DateNaissance.Date;
        var age = today.Year - birthDate.Year;

        if (birthDate > today.AddYears(-age))
        {
            age--;
        }

        return age < 18;
    }

    private static SexeCategory ClassifySexe(string? sexe)
    {
        var normalized = DatabaseText.NormalizeSearchKey(sexe ?? string.Empty);

        return normalized switch
        {
            "F" or "FEMININ" or "FILLE" => SexeCategory.Feminin,
            "M" or "MASCULIN" or "GARCON" => SexeCategory.Masculin,
            _ => SexeCategory.NonRenseigne
        };
    }

    private enum SexeCategory
    {
        NonRenseigne,
        Feminin,
        Masculin
    }
}
