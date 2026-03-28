using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
using MangoTaika.Helpers;
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
            .Include(b => b.ChefUnite)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (branche is null)
        {
            return null;
        }

        var normalizedNom = DatabaseText.NormalizeSearchKey(branche.Nom);
        var relatedBranches = (await db.Branches
                .Include(b => b.Groupe)
                .Include(b => b.ChefUnite)
                .Where(b => b.IsActive)
                .ToListAsync())
            .Where(b => DatabaseText.NormalizeSearchKey(b.Nom) == normalizedNom)
            .ToList();

        var relatedBranchIds = relatedBranches.Select(b => b.Id).ToList();
        var scouts = await db.Scouts
            .Where(s => s.IsActive && s.BrancheId.HasValue && relatedBranchIds.Contains(s.BrancheId.Value))
            .ToListAsync();

        return BuildDetailedDto(branche, relatedBranches, scouts);
    }

    public async Task<BrancheDto> CreateAsync(BrancheCreateDto dto)
    {
        var nom = NormalizeNom(dto.Nom);
        var description = NormalizeOptional(dto.Description);
        ValidateAges(dto.AgeMin, dto.AgeMax);
        await EnsureActiveGroupeExistsAsync(dto.GroupeId);
        await EnsureUniqueNomAsync(dto.GroupeId, nom);
        var chefUnite = await GetChefUniteAsync(dto.GroupeId, dto.ChefUniteId);

        var branche = new Branche
        {
            Id = Guid.NewGuid(),
            Nom = nom,
            Description = description,
            LogoUrl = NormalizeOptional(dto.LogoUrl),
            AgeMin = dto.AgeMin,
            AgeMax = dto.AgeMax,
            ChefUniteId = chefUnite.Id,
            NomChefUnite = $"{chefUnite.Prenom} {chefUnite.Nom}",
            GroupeId = dto.GroupeId
        };
        db.Branches.Add(branche);
        await SaveChangesAsync();
        return ToDto(branche);
    }

    public async Task<bool> UpdateAsync(Guid id, BrancheCreateDto dto)
    {
        var branche = await db.Branches.FindAsync(id);
        if (branche is null) return false;

        var nom = NormalizeNom(dto.Nom);
        var description = NormalizeOptional(dto.Description);
        ValidateAges(dto.AgeMin, dto.AgeMax);
        await EnsureActiveGroupeExistsAsync(dto.GroupeId);
        await EnsureUniqueNomAsync(dto.GroupeId, nom, id);
        var chefUnite = await GetChefUniteAsync(dto.GroupeId, dto.ChefUniteId);

        branche.Nom = nom;
        branche.Description = description;
        branche.LogoUrl = NormalizeOptional(dto.LogoUrl);
        branche.AgeMin = dto.AgeMin;
        branche.AgeMax = dto.AgeMax;
        branche.ChefUniteId = chefUnite.Id;
        branche.NomChefUnite = $"{chefUnite.Prenom} {chefUnite.Nom}";
        branche.GroupeId = dto.GroupeId;
        await SaveChangesAsync();
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
        LogoUrl = b.LogoUrl,
        AgeMin = b.AgeMin,
        AgeMax = b.AgeMax,
        NomChefUnite = b.ChefUnite != null ? $"{b.ChefUnite.Prenom} {b.ChefUnite.Nom}" : b.NomChefUnite,
        ChefUniteId = b.ChefUniteId,
        GroupeId = b.GroupeId,
        NomGroupe = b.Groupe?.Nom,
        ResponsablePhotoUrl = NormalizeOptional(b.ChefUnite?.PhotoUrl),
        NombreScouts = b.Scouts.Count(s => s.IsActive)
    };

    private static BrancheDto BuildDetailedDto(Branche selectedBranche, IReadOnlyCollection<Branche> relatedBranches, IReadOnlyCollection<Scout> scouts)
    {
        var dto = ToDto(selectedBranche);
        dto.NombreScouts = scouts.Count;
        dto.NombreFilles = scouts.Count(s => ClassifySexe(s.Sexe) == SexeCategory.Feminin);
        dto.NombreGarcons = scouts.Count(s => ClassifySexe(s.Sexe) == SexeCategory.Masculin);
        dto.Jeunes = BuildRepartition(scouts.Where(IsJeune));
        dto.Adultes = BuildRepartition(scouts.Where(s => !IsJeune(s)));

        var relatedBranchesById = relatedBranches.ToDictionary(b => b.Id);

        dto.TotauxParGroupes = relatedBranches
            .GroupBy(b => b.GroupeId)
            .Select(group =>
            {
                var referenceBranche = group.First();
                var groupBranchIds = group.Select(b => b.Id).ToHashSet();
                var groupScouts = scouts
                    .Where(s => s.BrancheId.HasValue && groupBranchIds.Contains(s.BrancheId.Value))
                    .ToList();

                return new BrancheGroupeSummaryDto
                {
                    GroupeId = group.Key,
                    NomGroupe = referenceBranche.Groupe?.Nom ?? "-",
                    LogoGroupeUrl = NormalizeOptional(referenceBranche.Groupe?.LogoUrl),
                    NombreScouts = groupScouts.Count,
                    NombreJeunes = groupScouts.Count(IsJeune),
                    NombreAdultes = groupScouts.Count(s => !IsJeune(s))
                };
            })
            .OrderBy(summary => summary.NomGroupe)
            .ToList();

        dto.Membres = scouts
            .OrderBy(s => relatedBranchesById.TryGetValue(s.BrancheId!.Value, out var branche) ? branche.Groupe?.Nom : string.Empty)
            .ThenBy(s => s.Nom)
            .ThenBy(s => s.Prenom)
            .Select(s =>
            {
                var branche = relatedBranchesById[s.BrancheId!.Value];
                return new BrancheMembreDto
                {
                    Matricule = s.Matricule,
                    Nom = s.Nom,
                    Prenoms = s.Prenom,
                    Groupe = branche.Groupe?.Nom ?? "-",
                    Fonction = NormalizeOptional(s.Fonction) ?? "-"
                };
            })
            .ToList();

        return dto;
    }

    private async Task<Scout> GetChefUniteAsync(Guid groupeId, Guid? chefUniteId)
    {
        if (!chefUniteId.HasValue)
        {
            throw new InvalidOperationException("Le chef d'unite est obligatoire.");
        }

        var chefUnite = await db.Scouts
            .FirstOrDefaultAsync(s => s.Id == chefUniteId.Value && s.IsActive);

        if (chefUnite is null)
        {
            throw new InvalidOperationException("Le chef d'unite selectionne est introuvable.");
        }

        if (chefUnite.GroupeId != groupeId)
        {
            throw new InvalidOperationException("Le chef d'unite doit appartenir au groupe selectionne.");
        }

        return chefUnite;
    }

    private async Task EnsureActiveGroupeExistsAsync(Guid groupeId)
    {
        if (groupeId == Guid.Empty)
        {
            throw new InvalidOperationException("Le groupe est obligatoire.");
        }

        var groupeExists = await db.Groupes.AnyAsync(g => g.Id == groupeId && g.IsActive);
        if (!groupeExists)
        {
            throw new InvalidOperationException("Le groupe selectionne est introuvable ou inactif.");
        }
    }

    private async Task EnsureUniqueNomAsync(Guid groupeId, string nom, Guid? currentBrancheId = null)
    {
        var normalizedNom = DatabaseText.NormalizeSearchKey(nom);
        var duplicateExists = db.Database.IsNpgsql()
            ? await db.Branches.AnyAsync(b =>
                b.IsActive &&
                b.GroupeId == groupeId &&
                b.Id != currentBrancheId &&
                b.NomNormalise == normalizedNom)
            : (await db.Branches
                .Where(b => b.IsActive && b.GroupeId == groupeId && b.Id != currentBrancheId)
                .Select(b => b.Nom)
                .ToListAsync())
                .Any(existingNom => DatabaseText.NormalizeSearchKey(existingNom) == normalizedNom);

        if (duplicateExists)
        {
            throw new InvalidOperationException("Une branche avec ce nom existe deja dans ce groupe.");
        }
    }

    private static string NormalizeNom(string nom)
    {
        var normalized = nom?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Le nom de la branche est obligatoire.");
        }

        return normalized;
    }

    private static void ValidateAges(int? ageMin, int? ageMax)
    {
        if (ageMin.HasValue && ageMin.Value < 0)
        {
            throw new InvalidOperationException("L'age minimum ne peut pas etre negatif.");
        }

        if (ageMax.HasValue && ageMax.Value < 0)
        {
            throw new InvalidOperationException("L'age maximum ne peut pas etre negatif.");
        }

        if (ageMin.HasValue && ageMax.HasValue && ageMin > ageMax)
        {
            throw new InvalidOperationException("L'age minimum ne peut pas etre superieur a l'age maximum.");
        }
    }

    private async Task SaveChangesAsync()
    {
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.ReferencesConstraint(PersistenceConstraints.BranchesGroupeNomNormaliseActif))
        {
            throw new InvalidOperationException("Une branche avec ce nom existe deja dans ce groupe.", ex);
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
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
