using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.Helpers;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Services;

public sealed class DistrictBranchInheritanceService(AppDbContext db)
{
    private const string DistrictGroupName = "Equipe de District Mango Taika";

    public async Task EnsureInheritedBranchesAsync()
    {
        var districtGroup = await GetDistrictGroupAsync();
        if (districtGroup is null)
        {
            return;
        }

        var districtBranches = await GetActiveBranchesForGroupAsync(districtGroup.Id);
        if (districtBranches.Count == 0)
        {
            return;
        }

        var otherGroups = await db.Groupes
            .Where(g => g.IsActive && g.Id != districtGroup.Id)
            .ToListAsync();

        if (otherGroups.Count == 0)
        {
            return;
        }

        var existingPairs = await db.Branches
            .Where(b => b.IsActive && otherGroups.Select(g => g.Id).Contains(b.GroupeId))
            .Select(b => new { b.GroupeId, b.Nom })
            .ToListAsync();

        var pairKeys = existingPairs
            .Select(pair => BuildPairKey(pair.GroupeId, pair.Nom))
            .ToHashSet(StringComparer.Ordinal);

        var hasChanges = false;
        foreach (var group in otherGroups)
        {
            hasChanges |= AddMissingBranchesForGroup(group.Id, districtBranches, pairKeys);
        }

        if (hasChanges)
        {
            await db.SaveChangesAsync();
        }
    }

    public async Task InheritDistrictBranchesForGroupAsync(Groupe groupe)
    {
        if (!groupe.IsActive || IsDistrictGroup(groupe.Nom))
        {
            return;
        }

        var districtGroup = await GetDistrictGroupAsync();
        if (districtGroup is null || districtGroup.Id == groupe.Id)
        {
            return;
        }

        var districtBranches = await GetActiveBranchesForGroupAsync(districtGroup.Id);
        if (districtBranches.Count == 0)
        {
            return;
        }

        var existingKeys = (await db.Branches
                .Where(b => b.IsActive && b.GroupeId == groupe.Id)
                .Select(b => b.Nom)
                .ToListAsync())
            .Select(nom => BuildPairKey(groupe.Id, nom))
            .ToHashSet(StringComparer.Ordinal);

        if (AddMissingBranchesForGroup(groupe.Id, districtBranches, existingKeys))
        {
            await db.SaveChangesAsync();
        }
    }

    public async Task PropagateDistrictBranchAsync(Branche branche)
    {
        var districtGroup = await GetDistrictGroupAsync();
        if (districtGroup is null || branche.GroupeId != districtGroup.Id || !branche.IsActive)
        {
            return;
        }

        var otherGroups = await db.Groupes
            .Where(g => g.IsActive && g.Id != districtGroup.Id)
            .ToListAsync();

        if (otherGroups.Count == 0)
        {
            return;
        }

        var existingPairs = await db.Branches
            .Where(b => b.IsActive && otherGroups.Select(g => g.Id).Contains(b.GroupeId))
            .Select(b => new { b.GroupeId, b.Nom })
            .ToListAsync();

        var pairKeys = existingPairs
            .Select(pair => BuildPairKey(pair.GroupeId, pair.Nom))
            .ToHashSet(StringComparer.Ordinal);

        var hasChanges = false;
        foreach (var group in otherGroups)
        {
            hasChanges |= AddMissingBranchesForGroup(group.Id, [branche], pairKeys);
        }

        if (hasChanges)
        {
            await db.SaveChangesAsync();
        }
    }

    private async Task<Groupe?> GetDistrictGroupAsync()
    {
        var groups = await db.Groupes
            .Where(g => g.IsActive)
            .ToListAsync();

        return groups.FirstOrDefault(g => IsDistrictGroup(g.Nom));
    }

    private async Task<List<Branche>> GetActiveBranchesForGroupAsync(Guid groupeId)
    {
        return await db.Branches
            .Where(b => b.IsActive && b.GroupeId == groupeId)
            .OrderBy(b => b.AgeMin)
            .ThenBy(b => b.Nom)
            .ToListAsync();
    }

    private bool AddMissingBranchesForGroup(Guid groupId, IReadOnlyCollection<Branche> templates, ISet<string> existingKeys)
    {
        var hasChanges = false;

        foreach (var template in templates)
        {
            var pairKey = BuildPairKey(groupId, template.Nom);
            if (existingKeys.Contains(pairKey))
            {
                continue;
            }

            db.Branches.Add(new Branche
            {
                Id = Guid.NewGuid(),
                Nom = template.Nom,
                Description = template.Description,
                LogoUrl = template.LogoUrl,
                AgeMin = template.AgeMin,
                AgeMax = template.AgeMax,
                ChefUniteId = null,
                NomChefUnite = null,
                GroupeId = groupId
            });

            existingKeys.Add(pairKey);
            hasChanges = true;
        }

        return hasChanges;
    }

    private static bool IsDistrictGroup(string? groupName)
    {
        return DatabaseText.NormalizeSearchKey(groupName) == DatabaseText.NormalizeSearchKey(DistrictGroupName);
    }

    private static string BuildPairKey(Guid groupId, string? branchName)
    {
        return $"{groupId:N}:{DatabaseText.NormalizeSearchKey(branchName)}";
    }
}
