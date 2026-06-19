namespace MangoTaika.Services;

public static class PermissionCodes
{
    public const string DashboardVoir = "Dashboard.Voir";
    public const string TerritoireScoutsVoir = "Territoire.Scouts.Voir";
    public const string TerritoireGroupesVoir = "Territoire.Groupes.Voir";
    public const string TerritoireBranchesVoir = "Territoire.Branches.Voir";
    public const string TerritoireRegionsVoir = "Territoire.Regions.Voir";
    public const string TerritoireCarteVoir = "Territoire.Carte.Voir";
    public const string TerritoireInscriptionsVoir = "Territoire.Inscriptions.Voir";
    public const string ActivitesVoir = "Activites.Voir";
    public const string RessourcesVoir = "Activites.Ressources.Voir";
    public const string UnitesVoir = "Activites.Unites.Voir";
    public const string CompetencesVoir = "Activites.Competences.Voir";
    public const string ProgrammesVoir = "Activites.Programmes.Voir";
    public const string RapportsActiviteVoir = "Activites.Rapports.Voir";
    public const string PropositionsMaitriseVoir = "Activites.PropositionsMaitrise.Voir";
    public const string FormationsVoir = "Formations.Voir";
    public const string FormationsStatistiquesVoir = "Formations.Statistiques.Voir";
    public const string DemandesVoir = "Demandes.Voir";
    public const string DemandesGroupeVoir = "DemandesGroupe.Voir";
    public const string FinancesVoir = "Finances.Voir";
    public const string DonsAdminVoir = "Finances.Dons.Voir";
    public const string PortefeuillesAdminVoir = "Finances.Portefeuilles.Voir";
    public const string ComptesPaiementVoir = "Finances.ComptesPaiement.Voir";
    public const string AbonnementsVoir = "Finances.Abonnements.Voir";
    public const string CotisationsVoir = "Finances.Cotisations.Voir";
    public const string AgrVoir = "Finances.AGR.Voir";
    public const string CommunicationVoir = "Communication.Voir";
    public const string MotCommissaireVoir = "Communication.MotCommissaire.Voir";
    public const string ActualitesAdminVoir = "Communication.Actualites.Voir";
    public const string GalerieAdminVoir = "Communication.Galerie.Voir";
    public const string PartenairesVoir = "Communication.Partenaires.Voir";
    public const string AdministrationVoir = "Administration.Voir";
    public const string UtilisateursVoir = "Administration.Utilisateurs.Voir";
    public const string RolesVoir = "Administration.Roles.Voir";
    public const string PermissionsVoir = "Administration.Permissions.Voir";
    public const string CodesInvitationVoir = "Administration.CodesInvitation.Voir";
    public const string MaintenanceVoir = "Administration.Maintenance.Voir";
    public const string MessagesVoir = "Administration.Messages.Voir";
    public const string HistoriqueVoir = "Administration.Historique.Voir";
    public const string SupportVoir = "Support.Voir";
    public const string TicketsVoir = "Support.Tickets.Voir";
    public const string CatalogueSupportVoir = "Support.Catalogue.Voir";
    public const string BaseConnaissancesVoir = "Support.BaseConnaissances.Voir";
    public const string ReportingVoir = "Reporting.Voir";

    public const string BoutiqueCatalogueVoir = "Boutique.Catalogue.Voir";
    public const string BoutiquePanierGerer = "Boutique.Panier.Gerer";
    public const string BoutiqueCommandesCreer = "Boutique.Commandes.Creer";
    public const string BoutiqueArticlesVoir = "Boutique.Articles.Voir";
    public const string BoutiqueArticlesCreer = "Boutique.Articles.Creer";
    public const string BoutiqueArticlesModifier = "Boutique.Articles.Modifier";
    public const string BoutiqueArticlesSupprimer = "Boutique.Articles.Supprimer";
    public const string BoutiqueCommandesVoir = "Boutique.Commandes.Voir";
    public const string BoutiqueCommandesValider = "Boutique.Commandes.Valider";
    public const string BoutiqueCommandesLivrer = "Boutique.Commandes.Livrer";
    public const string BoutiqueCommandesAnnuler = "Boutique.Commandes.Annuler";

    public static readonly IReadOnlyList<PermissionDefinition> All =
    [
        new(DashboardVoir, "Voir le tableau de bord", "Tableau de bord", "Acceder au tableau de bord de son espace."),
        new(TerritoireScoutsVoir, "Voir les scouts", "Territoire", "Consulter les fiches scouts selon le perimetre autorise."),
        new(TerritoireGroupesVoir, "Voir les entites scouts", "Territoire", "Consulter les groupes et entites scouts."),
        new(TerritoireBranchesVoir, "Voir les branches", "Territoire", "Consulter les branches selon le perimetre autorise."),
        new(TerritoireRegionsVoir, "Voir regions et districts", "Territoire", "Administrer l'organisation territoriale."),
        new(TerritoireCarteVoir, "Voir la carte GPS", "Territoire", "Consulter la carte des groupes."),
        new(TerritoireInscriptionsVoir, "Voir les inscriptions annuelles", "Territoire", "Suivre les inscriptions annuelles."),
        new(ActivitesVoir, "Voir les activites", "Activites & suivi", "Consulter les activites selon le perimetre autorise."),
        new(RessourcesVoir, "Voir les ressources", "Activites & suivi", "Gerer les ressources invite, formateur, parrain ou autres."),
        new(UnitesVoir, "Voir les unites scoutes", "Activites & suivi", "Consulter et administrer les unites scoutes."),
        new(CompetencesVoir, "Voir les competences", "Activites & suivi", "Consulter et administrer les competences."),
        new(ProgrammesVoir, "Voir les programmes annuels", "Activites & suivi", "Consulter et administrer les programmes annuels."),
        new(RapportsActiviteVoir, "Voir les rapports d'activite", "Activites & suivi", "Consulter les rapports post-activite."),
        new(PropositionsMaitriseVoir, "Voir les propositions de maitrise", "Activites & suivi", "Suivre les propositions de maitrise."),
        new(FormationsVoir, "Voir les formations", "Formations", "Acceder aux formations selon le role."),
        new(FormationsStatistiquesVoir, "Voir les statistiques formations", "Formations", "Consulter les statistiques des formations."),
        new(DemandesVoir, "Voir les demandes administratives", "Demandes & validations", "Consulter et traiter les demandes administratives."),
        new(DemandesGroupeVoir, "Voir les demandes de groupe", "Demandes & validations", "Consulter et traiter les demandes de groupe."),
        new(FinancesVoir, "Voir les finances", "Finances & AGR", "Consulter les mouvements financiers."),
        new(DonsAdminVoir, "Voir les dons", "Finances & AGR", "Consulter les dons declares."),
        new(PortefeuillesAdminVoir, "Voir les portefeuilles", "Finances & AGR", "Administrer les portefeuilles."),
        new(ComptesPaiementVoir, "Voir les comptes de paiement", "Finances & AGR", "Parametrer les comptes mobiles de paiement."),
        new(AbonnementsVoir, "Voir les profils abonnes", "Finances & AGR", "Administrer les profils d'abonnement."),
        new(CotisationsVoir, "Voir les cotisations nationales", "Finances & AGR", "Suivre les cotisations nationales."),
        new(AgrVoir, "Voir les projets AGR", "Finances & AGR", "Consulter et administrer les projets AGR."),
        new(CommunicationVoir, "Voir le module communication", "Communication", "Afficher le module communication."),
        new(MotCommissaireVoir, "Voir le mot du commissaire", "Communication", "Administrer le mot du commissaire."),
        new(ActualitesAdminVoir, "Voir les actualites admin", "Communication", "Administrer les actualites."),
        new(GalerieAdminVoir, "Voir la galerie admin", "Communication", "Administrer la galerie."),
        new(PartenairesVoir, "Voir partenaires et reseaux sociaux", "Communication", "Administrer les partenaires et liens sociaux."),
        new(AdministrationVoir, "Voir le module administration", "Administration", "Afficher le module administration."),
        new(UtilisateursVoir, "Voir les utilisateurs", "Administration", "Administrer les utilisateurs."),
        new(RolesVoir, "Voir roles et visibilite", "Administration", "Administrer les roles."),
        new(PermissionsVoir, "Voir permissions roles", "Administration", "Administrer les permissions par role."),
        new(CodesInvitationVoir, "Voir les codes d'invitation", "Administration", "Administrer les codes d'invitation."),
        new(MaintenanceVoir, "Voir la maintenance", "Administration", "Acceder aux outils de maintenance."),
        new(MessagesVoir, "Voir messages et avis", "Administration", "Valider et traiter les messages recus."),
        new(HistoriqueVoir, "Voir les membres historiques", "Administration", "Administrer les membres historiques."),
        new(SupportVoir, "Voir le support", "Support", "Afficher le module support."),
        new(TicketsVoir, "Voir les tickets", "Support", "Consulter et traiter les tickets."),
        new(CatalogueSupportVoir, "Voir catalogue de services", "Support", "Administrer le catalogue de services support."),
        new(BaseConnaissancesVoir, "Voir base de connaissances", "Support", "Administrer la base de connaissances."),
        new(ReportingVoir, "Voir le reporting", "Reporting", "Consulter les tableaux de reporting."),
        new(BoutiqueCatalogueVoir, "Voir le catalogue boutique", "Boutique", "Acceder au catalogue public de la boutique."),
        new(BoutiquePanierGerer, "Gerer le panier", "Boutique", "Ajouter, modifier ou retirer des articles du panier."),
        new(BoutiqueCommandesCreer, "Creer une commande boutique", "Boutique", "Valider un panier et creer une commande."),
        new(BoutiqueArticlesVoir, "Voir les articles en administration", "Boutique", "Consulter la liste admin des articles boutique."),
        new(BoutiqueArticlesCreer, "Creer/importer des articles", "Boutique", "Ajouter ou importer des articles boutique."),
        new(BoutiqueArticlesModifier, "Modifier des articles", "Boutique", "Modifier les fiches articles et leur stock."),
        new(BoutiqueArticlesSupprimer, "Supprimer des articles", "Boutique", "Masquer un article de la boutique par suppression logique."),
        new(BoutiqueCommandesVoir, "Voir les commandes boutique", "Boutique", "Consulter les commandes et leurs details."),
        new(BoutiqueCommandesValider, "Valider les commandes boutique", "Boutique", "Confirmer une commande et reserver/decompter le stock."),
        new(BoutiqueCommandesLivrer, "Marquer les commandes livrees", "Boutique", "Finaliser le traitement livraison d'une commande."),
        new(BoutiqueCommandesAnnuler, "Annuler les commandes boutique", "Boutique", "Annuler une commande et reintegrer le stock si besoin.")
    ];
}

public sealed record PermissionDefinition(string Code, string Libelle, string Module, string Description);
