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
            .Include(rp => rp.Permission)
            .Select(rp => new { rp.RoleId, rp.PermissionId, rp.Permission.Code })
            .ToListAsync();
        var existingPairs = rolePermissions
            .Select(rp => $"{rp.RoleId:N}:{rp.PermissionId:N}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rolesWithGlobalPermissions = rolePermissions
            .Where(rp => !rp.Code.StartsWith("Boutique.", StringComparison.OrdinalIgnoreCase))
            .Select(rp => rp.RoleId)
            .ToHashSet();

        foreach (var (roleName, codes) in BuildDefaultMatrix())
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                continue;
            }

            if (rolesWithGlobalPermissions.Contains(role.Id))
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
                PermissionCodes.DashboardVoir,
                PermissionCodes.ActivitesVoir,
                PermissionCodes.DemandesVoir,
                PermissionCodes.FormationsVoir,
                PermissionCodes.TicketsVoir,
                PermissionCodes.BoutiqueCatalogueVoir,
                PermissionCodes.BoutiquePanierGerer,
                PermissionCodes.BoutiqueCommandesCreer
            };

        var territoire =
            new[]
            {
                PermissionCodes.TerritoireScoutsVoir,
                PermissionCodes.TerritoireGroupesVoir,
                PermissionCodes.TerritoireBranchesVoir,
                PermissionCodes.TerritoireRegionsVoir,
                PermissionCodes.TerritoireCarteVoir,
                PermissionCodes.TerritoireInscriptionsVoir
            };

        var activites =
            new[]
            {
                PermissionCodes.ActivitesVoir,
                PermissionCodes.RessourcesVoir,
                PermissionCodes.UnitesVoir,
                PermissionCodes.CompetencesVoir,
                PermissionCodes.ProgrammesVoir,
                PermissionCodes.RapportsActiviteVoir,
                PermissionCodes.PropositionsMaitriseVoir,
                PermissionCodes.FormationsVoir,
                PermissionCodes.FormationsStatistiquesVoir
            };

        var demandes =
            new[]
            {
                PermissionCodes.DemandesVoir,
                PermissionCodes.DemandesCreer,
                PermissionCodes.DemandesModifier,
                PermissionCodes.DemandesSoumettre,
                PermissionCodes.DemandesValiderChefGroupe,
                PermissionCodes.DemandesValiderDistrict,
                PermissionCodes.DemandesRejeter,
                PermissionCodes.DemandesReviser,
                PermissionCodes.DemandesSupprimer,
                PermissionCodes.DemandesGroupeVoir
            };

        var demandesScopedLeader =
            new[]
            {
                PermissionCodes.DemandesVoir,
                PermissionCodes.DemandesCreer,
                PermissionCodes.DemandesModifier,
                PermissionCodes.DemandesSoumettre,
                PermissionCodes.DemandesValiderChefGroupe
            };

        var demandesUnitLeader =
            new[]
            {
                PermissionCodes.DemandesVoir,
                PermissionCodes.DemandesCreer,
                PermissionCodes.DemandesModifier,
                PermissionCodes.DemandesSoumettre
            };

        var finances =
            new[]
            {
                PermissionCodes.FinancesVoir,
                PermissionCodes.DonsAdminVoir,
                PermissionCodes.PortefeuillesAdminVoir,
                PermissionCodes.ComptesPaiementVoir,
                PermissionCodes.AbonnementsVoir,
                PermissionCodes.CotisationsVoir,
                PermissionCodes.AgrVoir
            };

        var communication =
            new[]
            {
                PermissionCodes.MotCommissaireVoir,
                PermissionCodes.ActualitesAdminVoir,
                PermissionCodes.GalerieAdminVoir,
                PermissionCodes.PartenairesVoir
            };

        var administration =
            new[]
            {
                PermissionCodes.UtilisateursVoir,
                PermissionCodes.RolesVoir,
                PermissionCodes.PermissionsVoir,
                PermissionCodes.CodesInvitationVoir,
                PermissionCodes.MaintenanceVoir,
                PermissionCodes.MessagesVoir,
                PermissionCodes.HistoriqueVoir
            };

        var support =
            new[]
            {
                PermissionCodes.TicketsVoir,
                PermissionCodes.CatalogueSupportVoir,
                PermissionCodes.BaseConnaissancesVoir
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

        var fullBackOffice =
            publicCustomer
                .Concat(territoire)
                .Concat(activites)
                .Concat(demandes)
                .Concat(finances)
                .Concat(communication)
                .Concat(administration)
                .Concat(support)
                .Concat(boutiqueManager)
                .Append(PermissionCodes.ReportingVoir)
                .Distinct()
                .ToArray();

        var districtLeadership =
            fullBackOffice
                .Except([PermissionCodes.MaintenanceVoir])
                .ToArray();

        var supervision =
            publicCustomer
                .Concat(territoire)
                .Concat([
                    PermissionCodes.ActivitesVoir,
                    PermissionCodes.CompetencesVoir,
                    PermissionCodes.ProgrammesVoir,
                    PermissionCodes.RapportsActiviteVoir,
                    PermissionCodes.FormationsVoir,
                    PermissionCodes.FormationsStatistiquesVoir,
                    PermissionCodes.DemandesVoir,
                    PermissionCodes.DemandesGroupeVoir,
                    PermissionCodes.FinancesVoir,
                    PermissionCodes.DonsAdminVoir,
                    PermissionCodes.CotisationsVoir,
                    PermissionCodes.AgrVoir,
                    PermissionCodes.HistoriqueVoir,
                    PermissionCodes.TicketsVoir,
                    PermissionCodes.ReportingVoir,
                    PermissionCodes.BoutiqueArticlesVoir,
                    PermissionCodes.BoutiqueCommandesVoir
                ])
                .Distinct()
                .ToArray();

        var scopedLeaderBase =
            publicCustomer
                .Concat([
                    PermissionCodes.TerritoireScoutsVoir,
                    PermissionCodes.TerritoireGroupesVoir,
                    PermissionCodes.TerritoireBranchesVoir,
                    PermissionCodes.ActivitesVoir,
                    PermissionCodes.RessourcesVoir,
                    PermissionCodes.UnitesVoir,
                    PermissionCodes.RapportsActiviteVoir,
                    PermissionCodes.PropositionsMaitriseVoir,
                    PermissionCodes.FormationsVoir
                ]);

        var chefGroupePermissions = scopedLeaderBase
            .Concat(demandesScopedLeader)
            .Distinct()
            .ToArray();

        var chefUnitePermissions = scopedLeaderBase
            .Concat(demandesUnitLeader)
            .Distinct()
            .ToArray();

        var scopedLeader = scopedLeaderBase.Distinct().ToArray();

        var supportAgent =
            publicCustomer
                .Concat(support)
                .Concat([
                    PermissionCodes.CatalogueSupportVoir,
                    PermissionCodes.BaseConnaissancesVoir
                ])
                .Distinct()
                .ToArray();

        return new Dictionary<string, string[]>
        {
            [RoleNames.Administrateur] = fullBackOffice,
            [RoleNames.Gestionnaire] = fullBackOffice,
            [RoleNames.CommissaireRegional] = districtLeadership,
            [RoleNames.EquipeRegionale] = scopedLeader,
            [RoleNames.CommissaireDistrict] = districtLeadership,
            [RoleNames.CommissaireDistrictAdjoint] = districtLeadership,
            [RoleNames.AssistantCommissaireDistrict] = districtLeadership,
            [RoleNames.EquipeDistrict] = scopedLeader,
            [RoleNames.ChefGroupe] = chefGroupePermissions,
            [RoleNames.ChefGroupeAdjoint] = chefGroupePermissions,
            [RoleNames.AssistantChefGroupe] = scopedLeader,
            [RoleNames.ChefUnite] = chefUnitePermissions,
            [RoleNames.ChefUniteAdjoint] = chefUnitePermissions,
            [RoleNames.AssistantChefUnite] = scopedLeader,
            [RoleNames.Scout] = publicCustomer,
            [RoleNames.Parent] = publicCustomer,
            [RoleNames.AgentSupport] = supportAgent,
            [RoleNames.Superviseur] = supervision,
            [RoleNames.Consultant] = supervision
        };
    }
}
