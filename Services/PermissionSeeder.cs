using MangoTaika.Data;
using MangoTaika.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Services;

public sealed class PermissionSeeder(
    AppDbContext db,
    RoleManager<IdentityRole<Guid>> roleManager)
{
    public async Task SeedAsync()
    {
        await SeedPermissionsAsync();
        await SeedRolePermissionsAsync();
    }

    private async Task SeedPermissionsAsync()
    {
        var existing = await db.Permissions.ToDictionaryAsync(p => p.Code);
        foreach (var definition in PermissionCodes.All)
        {
            if (existing.TryGetValue(definition.Code, out var permission))
            {
                permission.Libelle = definition.Libelle;
                permission.Module = definition.Module;
                permission.Description = definition.Description;
                permission.IsActive = true;
                continue;
            }

            db.Permissions.Add(new Permission
            {
                Id = Guid.NewGuid(),
                Code = definition.Code,
                Libelle = definition.Libelle,
                Module = definition.Module,
                Description = definition.Description,
                IsActive = true
            });
        }

        await db.SaveChangesAsync();
    }

    private async Task SeedRolePermissionsAsync()
    {
        var permissions = await db.Permissions.ToDictionaryAsync(p => p.Code);
        var rolePermissions = await db.RolePermissions
            .Select(rp => new { rp.RoleId, rp.PermissionId })
            .ToListAsync();
        var existingPairs = rolePermissions
            .Select(rp => $"{rp.RoleId:N}:{rp.PermissionId:N}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var (roleName, codes) in BuildDefaultMatrix())
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                continue;
            }

            foreach (var code in codes)
            {
                if (!permissions.TryGetValue(code, out var permission))
                {
                    continue;
                }

                var key = $"{role.Id:N}:{permission.Id:N}";
                if (existingPairs.Contains(key))
                {
                    continue;
                }

                db.RolePermissions.Add(new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = role.Id,
                    PermissionId = permission.Id
                });
                existingPairs.Add(key);
            }
        }

        await db.SaveChangesAsync();
    }

    private static IReadOnlyDictionary<string, string[]> BuildDefaultMatrix()
    {
        var publicCustomer =
            new[]
            {
                PermissionCodes.BoutiqueCatalogueVoir,
                PermissionCodes.BoutiquePanierGerer,
                PermissionCodes.BoutiqueCommandesCreer
            };

        var boutiqueManager =
            publicCustomer
                .Concat([
                    PermissionCodes.BoutiqueArticlesVoir,
                    PermissionCodes.BoutiqueArticlesCreer,
                    PermissionCodes.BoutiqueArticlesModifier,
                    PermissionCodes.BoutiqueArticlesSupprimer,
                    PermissionCodes.BoutiqueCommandesVoir,
                    PermissionCodes.BoutiqueCommandesValider,
                    PermissionCodes.BoutiqueCommandesLivrer,
                    PermissionCodes.BoutiqueCommandesAnnuler
                ])
                .Distinct()
                .ToArray();

        return new Dictionary<string, string[]>
        {
            [RoleNames.Administrateur] = boutiqueManager,
            [RoleNames.Gestionnaire] = boutiqueManager,
            [RoleNames.CommissaireDistrict] = boutiqueManager,
            [RoleNames.CommissaireDistrictAdjoint] = boutiqueManager,
            [RoleNames.AssistantCommissaireDistrict] = boutiqueManager,
            [RoleNames.EquipeDistrict] = [
                .. publicCustomer,
                PermissionCodes.BoutiqueArticlesVoir,
                PermissionCodes.BoutiqueCommandesVoir
            ],
            [RoleNames.ChefGroupe] = publicCustomer,
            [RoleNames.ChefUnite] = publicCustomer,
            [RoleNames.Scout] = publicCustomer,
            [RoleNames.Parent] = publicCustomer,
            [RoleNames.AgentSupport] = publicCustomer,
            [RoleNames.Superviseur] = [
                .. publicCustomer,
                PermissionCodes.BoutiqueArticlesVoir,
                PermissionCodes.BoutiqueCommandesVoir
            ],
            [RoleNames.Consultant] = [
                .. publicCustomer,
                PermissionCodes.BoutiqueArticlesVoir,
                PermissionCodes.BoutiqueCommandesVoir
            ]
        };
    }
}
