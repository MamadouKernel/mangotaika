using System.Globalization;
using System.Text;

namespace MangoTaika.Services;

public class ChatBotIntentService(IChatBotService chatBotService) : IChatBotIntentService
{
    private static readonly string[] MotsAgent = ["agent", "humain", "conseiller", "quelqu un", "parler a quelqu", "personne reelle", "operateur", "support humain"];
    private static readonly string[] MotsTickets = ["mes tickets", "voir mes ticket", "liste de mes ticket", "etat de mon ticket", "statut de mon ticket", "suivi de mon ticket"];
    private static readonly string[] MotsDemandes = ["demande d autorisation", "demande autorisation", "autorisation", "ma demande", "mes demandes", "statut de ma demande"];
    private static readonly string[] MotsMesActivites = ["mes activites", "mon activite", "mon inscription", "je participe", "ou j ai inscrit", "mes camps", "mes sorties"];
    private static readonly string[] MotsBoutique = ["boutique", "article", "prix", "acheter", "commander", "achat", "tarif", "magasin", "uniforme", "foulard", "insigne"];
    private static readonly string[] MotsActivitesPubliques = ["activite", "camp", "sortie", "evenement", "calendrier", "programme", "agenda", "prochain"];
    private static readonly string[] MotsActualites = ["actualite", "nouvelle", "news", "info recente", "derniere info", "quoi de neuf"];
    private static readonly string[] MotsResolu = ["c est resolu", "ca marche", "ca fonctionne", "merci ca marche", "probleme regle", "tout est bon", "c est bon", "c est regle", "parfait merci"];
    private static readonly string[] MotsIncidentGenerique = ["probleme", "bug", "panne", "ne marche pas", "ne fonctionne pas", "erreur", "incident", "support technique", "souci technique", "dysfonctionnement", "anomalie"];

    private static readonly (string[] MotsCles, string Titre, string Astuce)[] AidesItsm =
    [
        (["mot de passe", "connexion impossible", "je ne peux pas me connecter", "compte bloque", "identifiants", "code d acces oublie", "impossible de me connecter"],
            "Connexion / mot de passe",
            "Sur la page de connexion, cliquez sur \"Mot de passe oublié\" pour le réinitialiser. Si le compte reste bloqué, patientez 15 minutes avant de réessayer."),
        (["wifi", "pas de connexion internet", "reseau ne marche pas", "pas d internet", "deconnecte du reseau", "internet coupe"],
            "Réseau / Internet",
            "Vérifiez que le Wi-Fi est activé et que vous êtes connecté au bon réseau. Redémarrez le routeur si le problème persiste."),
        (["imprimante", "impression", "imprimer", "scanner ne marche pas"],
            "Imprimante / scanner",
            "Vérifiez que l'imprimante est allumée et bien connectée, puis redémarrez la file d'impression depuis les paramètres."),
        (["lent", "lenteur", "rame", "se bloque", "se fige", "plante", "tres lent", "freeze"],
            "Lenteur / blocage",
            "Fermez les applications inutilisées et redémarrez l'appareil. Si cela persiste après redémarrage, il peut s'agir d'un problème matériel."),
        (["email", "courriel", "boite mail", "messagerie ne marche pas", "je ne recois pas mes mails", "mail bloque"],
            "Messagerie",
            "Vérifiez votre connexion internet et que votre boîte n'est pas pleine. Déconnectez-vous puis reconnectez-vous à votre messagerie."),
        (["vpn", "acces a distance", "connexion a distance", "teletravail ne marche pas"],
            "VPN / accès à distance",
            "Vérifiez que le client VPN est bien installé et à jour, et que vos identifiants sont corrects. Redémarrez l'application VPN si la connexion échoue."),
        (["acces refuse", "je n ai pas acces", "permission refusee", "acces interdit", "pas les droits"],
            "Accès refusé",
            "Vérifiez que vous êtes connecté avec le bon compte et que votre rôle dispose des droits nécessaires. Le droit d'accès peut prendre quelques minutes à s'activer après une modification."),
        (["mise a jour", "installation", "logiciel ne s installe pas", "mettre a jour l application"],
            "Mise à jour / installation",
            "Redémarrez l'appareil puis relancez l'installation ou la mise à jour. Vérifiez aussi l'espace de stockage disponible."),
        (["fichier perdu", "document perdu", "sauvegarde", "fichier supprime par erreur", "document introuvable"],
            "Fichier perdu / sauvegarde",
            "Vérifiez la corbeille ou l'historique des versions du document. Si le fichier reste introuvable, un agent pourra vérifier les sauvegardes."),
        (["son ne marche pas", "pas de son", "micro ne marche pas", "camera ne marche pas", "probleme visio", "probleme video"],
            "Audio / vidéo",
            "Vérifiez que le bon périphérique (micro/caméra/haut-parleur) est sélectionné dans les paramètres, et que l'application a bien l'autorisation d'y accéder."),
        (["cle usb", "souris ne marche pas", "clavier ne marche pas", "peripherique non reconnu"],
            "Périphérique externe",
            "Débranchez puis rebranchez le périphérique, idéalement sur un autre port. Essayez-le sur un autre appareil pour vérifier s'il est défectueux.")
    ];

    public async Task<ChatBotReplyDto> InterpreterAsync(string message, Guid? userId, bool estAgent)
    {
        var texte = Normaliser(message);

        if (string.IsNullOrWhiteSpace(texte))
        {
            return new ChatBotReplyDto("Je n'ai pas compris. Posez-moi une question sur la boutique, les activités, vos tickets ou demandes.", "vide");
        }

        if (Contient(texte, MotsAgent))
        {
            return userId is null
                ? new ChatBotReplyDto("Connectez-vous pour être mis en relation avec un agent.", "agent-non-connecte")
                : new ChatBotReplyDto("Je vous mets en relation avec un agent, un instant...", "agent");
        }

        if (Contient(texte, MotsResolu))
        {
            return new ChatBotReplyDto("Parfait, ravi d'avoir pu aider ! N'hésitez pas si besoin.", "resolu-confirme");
        }

        foreach (var (motsCles, titre, astuce) in AidesItsm)
        {
            if (Contient(texte, motsCles))
            {
                return new ChatBotReplyDto(
                    $"{titre} — {astuce}\n\nCela résout-il votre problème ?",
                    "incident-aide",
                    ProblemeOriginal: message);
            }
        }

        if (Contient(texte, MotsIncidentGenerique))
        {
            return userId is null
                ? new ChatBotReplyDto("Connectez-vous pour qu'on puisse créer un ticket de support pour vous.", "besoin-connexion")
                : new ChatBotReplyDto(
                    "Je n'ai pas de solution toute prête pour ce cas. Je peux créer un ticket et vous mettre en relation avec un agent.",
                    "incident-generique",
                    ProblemeOriginal: message);
        }

        if (Contient(texte, MotsTickets))
        {
            if (userId is null) return BesoinConnexion("vos tickets");
            var items = await chatBotService.GetMesTicketsAsync(userId.Value);
            return Formater(items, "Voici vos derniers tickets :", "Vous n'avez aucun ticket pour le moment.", "tickets",
                it => new ChatBotCard(it.Titre, it.Date.ToString("d MMM yyyy", CultureInfo.GetCultureInfo("fr-FR")), TraduireStatut(it.Statut)));
        }

        if (Contient(texte, MotsDemandes))
        {
            if (userId is null) return BesoinConnexion("vos demandes");
            var items = await chatBotService.GetMesDemandesAsync(userId.Value);
            return Formater(items, "Voici vos dernières demandes d'autorisation :", "Vous n'avez aucune demande pour le moment.", "demandes",
                it => new ChatBotCard(it.Titre, it.Date.ToString("d MMM yyyy", CultureInfo.GetCultureInfo("fr-FR")), TraduireStatut(it.Statut)));
        }

        if (Contient(texte, MotsMesActivites))
        {
            if (userId is null) return BesoinConnexion("vos activités");
            var items = await chatBotService.GetMesActivitesAsync(userId.Value);
            return Formater(items, "Voici vos activités :", "Vous n'avez créé aucune activité pour le moment.", "mes-activites",
                it => new ChatBotCard(it.Titre, it.Date.ToString("d MMM yyyy", CultureInfo.GetCultureInfo("fr-FR")), TraduireStatut(it.Statut)));
        }

        if (Contient(texte, MotsBoutique))
        {
            var items = await chatBotService.GetArticlesBoutiqueAsync();
            return Formater(items, "Voici les articles disponibles à la boutique :", "Aucun article disponible pour le moment.", "boutique",
                it => new ChatBotCard(it.Nom, it.Categorie, $"{it.Prix:N0} {it.Devise}" + (it.EnStock ? "" : " (rupture)")));
        }

        if (Contient(texte, MotsActualites))
        {
            var items = await chatBotService.GetActualitesRecentesAsync();
            return Formater(items, "Voici les dernières actualités :", "Aucune actualité publiée pour le moment.", "actualites",
                it => new ChatBotCard(it.Titre, it.Resume, it.DatePublication.ToString("d MMM yyyy", CultureInfo.GetCultureInfo("fr-FR"))));
        }

        if (Contient(texte, MotsActivitesPubliques))
        {
            var items = await chatBotService.GetActivitesPubliquesAsync();
            return Formater(items, "Voici les prochaines activités :", "Aucune activité à venir pour le moment.", "activites-publiques",
                it => new ChatBotCard(it.Titre, it.Statut, it.Date.ToString("d MMM yyyy", CultureInfo.GetCultureInfo("fr-FR"))));
        }

        var faq = await chatBotService.RechercherFaqAsync(message);
        if (faq.Count > 0)
        {
            var cards = faq.Take(3).Select(f => new ChatBotCard(f.Question, f.Reponse, null)).ToList();
            return new ChatBotReplyDto("Voici ce que j'ai trouvé :", "faq", cards);
        }

        return new ChatBotReplyDto(
            "Je n'ai pas trouvé de réponse à votre question. Voulez-vous parler à un agent ?",
            "aucun-resultat");
    }

    private static ChatBotReplyDto BesoinConnexion(string quoi)
        => new($"Connectez-vous pour consulter {quoi}.", "besoin-connexion");

    private static ChatBotReplyDto Formater<T>(List<T> items, string introSiResultats, string messageSiVide, string intention, Func<T, ChatBotCard> mapper)
    {
        if (items.Count == 0)
            return new ChatBotReplyDto(messageSiVide, intention);

        return new ChatBotReplyDto(introSiResultats, intention, items.Select(mapper).ToList());
    }

    private static string TraduireStatut(string statut) => statut switch
    {
        "Ouvert" or "Nouveau" => "Ouvert",
        "Affecte" or "EnCours" => "En cours",
        "EnAttente" => "En attente",
        "Resolu" => "Résolu",
        "Ferme" => "Fermé",
        "Annule" => "Annulé",
        "Brouillon" => "Brouillon",
        "Soumise" => "Soumise",
        "Validee" => "Validée",
        "Rejetee" => "Rejetée",
        "Terminee" => "Terminée",
        "Archivee" => "Archivée",
        "Initialisee" => "Initialisée",
        "EnRevision" => "En révision",
        _ => statut
    };

    private static bool Contient(string texteNormalise, string[] mots)
        => mots.Any(m => texteNormalise.Contains(Normaliser(m)));

    private static string Normaliser(string texte)
    {
        var formeDecomposee = texte.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var c in formeDecomposee)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }
        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
