namespace MangoTaika.Services;

public static class PermissionCodes
{
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
