namespace MangoTaika.Services;

public static class PermissionRouteMap
{
    private static readonly Dictionary<string, string> ControllerPermissions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Abonnements"] = PermissionCodes.AbonnementsVoir,
        ["AGR"] = PermissionCodes.AgrVoir,
        ["Activites"] = PermissionCodes.ActivitesVoir,
        ["Branches"] = PermissionCodes.TerritoireBranchesVoir,
        ["Competences"] = PermissionCodes.CompetencesVoir,
        ["CotisationsNationales"] = PermissionCodes.CotisationsVoir,
        ["Dashboard"] = PermissionCodes.DashboardVoir,
        ["Demandes"] = PermissionCodes.DemandesVoir,
        ["Finances"] = PermissionCodes.FinancesVoir,
        ["Formations"] = PermissionCodes.FormationsVoir,
        ["ForumFormations"] = PermissionCodes.FormationsVoir,
        ["Galerie"] = PermissionCodes.GalerieAdminVoir,
        ["Groupes"] = PermissionCodes.TerritoireGroupesVoir,
        ["InscriptionsAnnuelles"] = PermissionCodes.TerritoireInscriptionsVoir,
        ["KnowledgeBase"] = PermissionCodes.BaseConnaissancesVoir,
        ["Maintenance"] = PermissionCodes.MaintenanceVoir,
        ["Messages"] = PermissionCodes.MessagesVoir,
        ["MotCommissaire"] = PermissionCodes.MotCommissaireVoir,
        ["Partenaires"] = PermissionCodes.PartenairesVoir,
        ["Permissions"] = PermissionCodes.PermissionsVoir,
        ["ProgrammesAnnuels"] = PermissionCodes.ProgrammesVoir,
        ["PropositionsMaitrise"] = PermissionCodes.PropositionsMaitriseVoir,
        ["RapportsActivite"] = PermissionCodes.RapportsActiviteVoir,
        ["Reporting"] = PermissionCodes.ReportingVoir,
        ["Ressources"] = PermissionCodes.RessourcesVoir,
        ["Roles"] = PermissionCodes.RolesVoir,
        ["Scouts"] = PermissionCodes.TerritoireScoutsVoir,
        ["SupportCatalog"] = PermissionCodes.CatalogueSupportVoir,
        ["Territoires"] = PermissionCodes.TerritoireRegionsVoir,
        ["Tickets"] = PermissionCodes.TicketsVoir,
        ["Unites"] = PermissionCodes.UnitesVoir
    };

    private static readonly Dictionary<(string Controller, string Action), string> ActionPermissions = new()
    {
        [("Account", "Utilisateurs")] = PermissionCodes.UtilisateursVoir,
        [("Account", "UtilisateurDetails")] = PermissionCodes.UtilisateursVoir,
        [("Account", "EditerUtilisateur")] = PermissionCodes.UtilisateursVoir,
        [("Account", "ActivateUser")] = PermissionCodes.UtilisateursVoir,
        [("Account", "DeactivateUser")] = PermissionCodes.UtilisateursVoir,
        [("Account", "UnlockUser")] = PermissionCodes.UtilisateursVoir,
        [("Account", "SoftDeleteUser")] = PermissionCodes.UtilisateursVoir,
        [("Account", "CodesInvitation")] = PermissionCodes.CodesInvitationVoir,
        [("Account", "DetailsCodeInvitation")] = PermissionCodes.CodesInvitationVoir,
        [("Account", "GenererCode")] = PermissionCodes.CodesInvitationVoir,
        [("Actualites", "Admin")] = PermissionCodes.ActualitesAdminVoir,
        [("Actualites", "Create")] = PermissionCodes.ActualitesAdminVoir,
        [("Actualites", "Edit")] = PermissionCodes.ActualitesAdminVoir,
        [("Actualites", "Publier")] = PermissionCodes.ActualitesAdminVoir,
        [("Actualites", "Depublier")] = PermissionCodes.ActualitesAdminVoir,
        [("Actualites", "Delete")] = PermissionCodes.ActualitesAdminVoir,
        [("DemandesGroupe", "Index")] = PermissionCodes.DemandesGroupeVoir,
        [("DemandesGroupe", "Details")] = PermissionCodes.DemandesGroupeVoir,
        [("DemandesGroupe", "Approuver")] = PermissionCodes.DemandesGroupeVoir,
        [("DemandesGroupe", "Rejeter")] = PermissionCodes.DemandesGroupeVoir,
        [("Dons", "Index")] = PermissionCodes.DonsAdminVoir,
        [("Dons", "Details")] = PermissionCodes.DonsAdminVoir,
        [("Dons", "Confirmer")] = PermissionCodes.DonsAdminVoir,
        [("Dons", "Rejeter")] = PermissionCodes.DonsAdminVoir,
        [("Dons", "Recu")] = PermissionCodes.DonsAdminVoir,
        [("Groupes", "Carte")] = PermissionCodes.TerritoireCarteVoir,
        [("Formations", "Statistiques")] = PermissionCodes.FormationsStatistiquesVoir,
        [("Historique", "Index")] = PermissionCodes.HistoriqueVoir,
        [("Historique", "Details")] = PermissionCodes.HistoriqueVoir,
        [("Historique", "Create")] = PermissionCodes.HistoriqueVoir,
        [("Historique", "Edit")] = PermissionCodes.HistoriqueVoir,
        [("Historique", "Delete")] = PermissionCodes.HistoriqueVoir,
        [("Portefeuilles", "Index")] = PermissionCodes.PortefeuillesAdminVoir,
        [("Portefeuilles", "Details")] = PermissionCodes.PortefeuillesAdminVoir,
        [("Portefeuilles", "Valider")] = PermissionCodes.PortefeuillesAdminVoir,
        [("Portefeuilles", "Rejeter")] = PermissionCodes.PortefeuillesAdminVoir,
        [("Portefeuilles", "CreerMouvement")] = PermissionCodes.PortefeuillesAdminVoir,
        [("Portefeuilles", "Recu")] = PermissionCodes.PortefeuillesAdminVoir,
        [("Portefeuilles", "ComptesPaiement")] = PermissionCodes.ComptesPaiementVoir,
        [("Portefeuilles", "EnregistrerComptePaiement")] = PermissionCodes.ComptesPaiementVoir,
        [("Portefeuilles", "SupprimerComptePaiement")] = PermissionCodes.ComptesPaiementVoir
    };

    public static string? Resolve(string? controller, string? action)
    {
        if (string.IsNullOrWhiteSpace(controller))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(action)
            && ActionPermissions.TryGetValue((controller, action), out var actionPermission))
        {
            return actionPermission;
        }

        return ControllerPermissions.TryGetValue(controller, out var controllerPermission)
            ? controllerPermission
            : null;
    }
}
