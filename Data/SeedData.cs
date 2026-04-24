using MangoTaika.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // Ne seeder que si la base est vide (hors admin)
        if (await db.Groupes.AnyAsync()) return;

        // ============================================================
        // 1. UTILISATEURS (Gestionnaire, Superviseur, Scout, Parent)
        // ============================================================
        var gestionnaire = await CreateUser(userManager, "0707070701", "Koné", "Aminata", "aminata.kone@email.ci", "Gestionnaire");
        var superviseur = await CreateUser(userManager, "0707070702", "Touré", "Ibrahim", "ibrahim.toure@email.ci", "Superviseur");
        var scoutUser1 = await CreateUser(userManager, "0707070703", "Diabaté", "Moussa", "moussa.diabate@email.ci", "Scout");
        var scoutUser2 = await CreateUser(userManager, "0707070704", "Coulibaly", "Fatou", "fatou.coulibaly@email.ci", "Scout");
        var parentUser = await CreateUser(userManager, "0707070705", "Ouattara", "Mariam", "mariam.ouattara@email.ci", "Parent");
        var consultant = await CreateUser(userManager, "0707070706", "Bamba", "Seydou", "seydou.bamba@email.ci", "Consultant");

        // Récupérer l'admin existant
        var admin = await db.Users.FirstAsync(u => u.Nom == "Admin");

        // ============================================================
        // 2. GROUPES (4 groupes scouts)
        // ============================================================
        var groupes = new[]
        {
            new Groupe { Id = Guid.NewGuid(), Nom = "Groupe 1er Abidjan", Description = "Premier groupe scout du district d'Abidjan, fondé en 1985.", Adresse = "Cocody, Abidjan", Latitude = 5.3364, Longitude = -3.9628, ResponsableId = gestionnaire.Id, NomChefGroupe = "Koné Drissa" },
            new Groupe { Id = Guid.NewGuid(), Nom = "Groupe Saint-Michel", Description = "Groupe scout rattaché à la paroisse Saint-Michel de Marcory.", Adresse = "Marcory, Abidjan", Latitude = 5.3000, Longitude = -3.9833, ResponsableId = gestionnaire.Id, NomChefGroupe = "Traoré Awa" },
            new Groupe { Id = Guid.NewGuid(), Nom = "Groupe Étoile du Sud", Description = "Groupe scout communautaire du quartier Treichville.", Adresse = "Treichville, Abidjan", Latitude = 5.2950, Longitude = -3.9970, NomChefGroupe = "Diallo Mamadou" },
            new Groupe { Id = Guid.NewGuid(), Nom = "Groupe Les Pionniers", Description = "Groupe scout de Yopougon, actif depuis 2010.", Adresse = "Yopougon, Abidjan", Latitude = 5.3300, Longitude = -4.0700 }
        };
        db.Groupes.AddRange(groupes);
        await db.SaveChangesAsync();

        // ============================================================
        // 3. BRANCHES (par groupe)
        // ============================================================
        var branches = new List<Branche>();
        var brancheNames = new[] {
            ("Louveteaux", "Branche des 8-12 ans", 8, 12),
            ("Éclaireurs", "Branche des 12-17 ans", 12, 17),
            ("Routiers", "Branche des 17-25 ans", 17, 25)
        };
        foreach (var g in groupes)
        {
            foreach (var (nom, desc, ageMin, ageMax) in brancheNames)
            {
                branches.Add(new Branche { Id = Guid.NewGuid(), Nom = nom, Description = desc, AgeMin = ageMin, AgeMax = ageMax, GroupeId = g.Id });
            }
        }
        db.Branches.AddRange(branches);
        await db.SaveChangesAsync();

        // ============================================================
        // 4. SCOUTS (20 scouts répartis dans les groupes/branches)
        // ============================================================
        var now = DateTime.UtcNow;
        var scoutData = new (string Nom, string Prenom, string Sexe, int AgeOffset, string Fonction, int grpIdx, int brIdx)[]
        {
            ("Diabaté", "Moussa", "M", -16, "Chef de patrouille", 0, 1),
            ("Coulibaly", "Fatou", "F", -15, "Seconde de patrouille", 0, 1),
            ("Konaté", "Adama", "M", -10, "", 0, 0),
            ("Traoré", "Aïssatou", "F", -11, "", 0, 0),
            ("Yao", "Kouadio", "M", -20, "Chef d'équipe", 0, 2),
            ("Diallo", "Kadiatou", "F", -14, "", 1, 1),
            ("Bamba", "Issouf", "M", -13, "", 1, 1),
            ("Ouattara", "Rokia", "F", -9, "", 1, 0),
            ("Sanogo", "Drissa", "M", -19, "Responsable routier", 1, 2),
            ("Koné", "Mariame", "F", -12, "", 1, 0),
            ("Cissé", "Abdoulaye", "M", -17, "Chef de patrouille", 2, 1),
            ("Dembélé", "Fatoumata", "F", -10, "", 2, 0),
            ("Fofana", "Mamadou", "M", -22, "ACD", 2, 2),
            ("Soro", "Aminata", "F", -15, "", 2, 1),
            ("Touré", "Lassina", "M", -11, "", 2, 0),
            ("Camara", "Djénéba", "F", -18, "Commissaire adjointe", 3, 2),
            ("Kouyaté", "Sékou", "M", -14, "", 3, 1),
            ("Doumbia", "Awa", "F", -13, "", 3, 1),
            ("Sidibé", "Boubacar", "M", -9, "", 3, 0),
            ("Keita", "Oumou", "F", -21, "Chef d'équipe", 3, 2)
        };

        var scouts = new List<Scout>();
        for (int i = 0; i < scoutData.Length; i++)
        {
            var (nom, prenom, sexe, ageOffset, fonction, grpIdx, brIdx) = scoutData[i];
            var branche = branches[grpIdx * 3 + brIdx];
            var scout = new Scout
            {
                Id = Guid.NewGuid(),
                Matricule = $"{583753 + i:D7}X",
                NumeroCarte = $"CI-{(1000 + i):D5}",
                Nom = nom,
                Prenom = prenom,
                DateNaissance = now.AddYears(ageOffset).AddMonths(-i),
                LieuNaissance = i % 3 == 0 ? "Abidjan" : i % 3 == 1 ? "Bouaké" : "Yamoussoukro",
                Sexe = sexe,
                Telephone = i < 2 ? (i == 0 ? "0707070703" : "0707070704") : null,
                Email = i < 2 ? (i == 0 ? "moussa.diabate@email.ci" : "fatou.coulibaly@email.ci") : null,
                RegionScoute = "Abidjan",
                District = "District MANGO TAÏKA",
                Fonction = string.IsNullOrEmpty(fonction) ? null : fonction,
                StatutASCCI = i % 4 == 0 ? "Enregistré" : "En attente",
                AssuranceAnnuelle = i % 3 != 2,
                GroupeId = groupes[grpIdx].Id,
                BrancheId = branche.Id,
                DateInscription = now.AddMonths(-(20 - i)),
                UserId = i == 0 ? scoutUser1.Id : i == 1 ? scoutUser2.Id : null
            };
            scouts.Add(scout);
        }
        db.Scouts.AddRange(scouts);
        await db.SaveChangesAsync();

        // Mettre à jour les chefs d'unité sur certaines branches
        branches[1].ChefUniteId = scouts[0].Id; // Éclaireurs Groupe 1
        branches[1].NomChefUnite = "Diabaté Moussa";
        branches[7].ChefUniteId = scouts[8].Id; // Routiers Groupe 2
        branches[7].NomChefUnite = "Sanogo Drissa";
        await db.SaveChangesAsync();

        // ============================================================
        // 5. PARENTS (3 parents liés à des scouts)
        // ============================================================
        var parents = new[]
        {
            new Parent { Id = Guid.NewGuid(), Nom = "Ouattara", Prenom = "Mariam", Telephone = "0707070705", Email = "mariam.ouattara@email.ci", Relation = "Mère", Scouts = { scouts[2], scouts[3] } },
            new Parent { Id = Guid.NewGuid(), Nom = "Diabaté", Prenom = "Seydou", Telephone = "0505050501", Email = "seydou.diabate@email.ci", Relation = "Père", Scouts = { scouts[0] } },
            new Parent { Id = Guid.NewGuid(), Nom = "Coulibaly", Prenom = "Aminata", Telephone = "0505050502", Relation = "Mère", Scouts = { scouts[1], scouts[6] } }
        };
        db.Parents.AddRange(parents);
        await db.SaveChangesAsync();

        // ============================================================
        // 6. ACTIVITÉS (8 activités variées)
        // ============================================================
        var activites = new[]
        {
            new Activite { Id = Guid.NewGuid(), Titre = "Camp de Pâques 2026", Description = "Camp annuel de Pâques pour les éclaireurs et routiers. Thème : Citoyenneté et environnement.", Type = TypeActivite.Camp, DateDebut = now.AddDays(15), DateFin = now.AddDays(18), Lieu = "Grand-Bassam", BudgetPrevisionnel = 750000, NomResponsable = "Koné Aminata", Statut = StatutActivite.Validee, CreateurId = gestionnaire.Id, GroupeId = groupes[0].Id },
            new Activite { Id = Guid.NewGuid(), Titre = "Sortie nature à Bingerville", Description = "Randonnée et découverte de la faune au jardin botanique.", Type = TypeActivite.Sortie, DateDebut = now.AddDays(-10), DateFin = now.AddDays(-10), Lieu = "Jardin botanique de Bingerville", BudgetPrevisionnel = 150000, NomResponsable = "Touré Ibrahim", Statut = StatutActivite.Terminee, CreateurId = gestionnaire.Id, GroupeId = groupes[0].Id },
            new Activite { Id = Guid.NewGuid(), Titre = "Réunion de rentrée", Description = "Réunion de planification pour la saison scoute 2026.", Type = TypeActivite.Reunion, DateDebut = now.AddDays(-30), Lieu = "Local du groupe Saint-Michel", Statut = StatutActivite.Terminee, CreateurId = gestionnaire.Id, GroupeId = groupes[1].Id },
            new Activite { Id = Guid.NewGuid(), Titre = "Formation premiers secours", Description = "Formation PSC1 dispensée par la Croix-Rouge de Côte d'Ivoire.", Type = TypeActivite.Formation, DateDebut = now.AddDays(5), DateFin = now.AddDays(6), Lieu = "Centre Croix-Rouge, Plateau", BudgetPrevisionnel = 200000, NomResponsable = "Dr. Konan", Statut = StatutActivite.Validee, CreateurId = admin.Id, GroupeId = null },
            new Activite { Id = Guid.NewGuid(), Titre = "Cérémonie de la Promesse", Description = "Cérémonie solennelle de la Promesse scoute pour les nouveaux louveteaux.", Type = TypeActivite.Ceremonie, DateDebut = now.AddDays(25), Lieu = "Paroisse Saint-Michel, Marcory", Statut = StatutActivite.Soumise, CreateurId = gestionnaire.Id, GroupeId = groupes[1].Id },
            new Activite { Id = Guid.NewGuid(), Titre = "Opération ville propre", Description = "Action communautaire de nettoyage du quartier Treichville.", Type = TypeActivite.Autre, DateDebut = now.AddDays(-5), Lieu = "Treichville, Abidjan", Statut = StatutActivite.Terminee, CreateurId = gestionnaire.Id, GroupeId = groupes[2].Id },
            new Activite { Id = Guid.NewGuid(), Titre = "Camp chantier Yopougon", Description = "Camp chantier de construction d'un local scout.", Type = TypeActivite.Camp, DateDebut = now.AddDays(40), DateFin = now.AddDays(45), Lieu = "Yopougon Niangon", BudgetPrevisionnel = 1200000, NomResponsable = "Camara Djénéba", Statut = StatutActivite.Brouillon, CreateurId = gestionnaire.Id, GroupeId = groupes[3].Id },
            new Activite { Id = Guid.NewGuid(), Titre = "Veillée culturelle", Description = "Soirée de chants, danses et contes traditionnels ivoiriens.", Type = TypeActivite.Autre, DateDebut = now.AddDays(8), Lieu = "Espace culturel Cocody", BudgetPrevisionnel = 100000, Statut = StatutActivite.Validee, CreateurId = admin.Id, GroupeId = groupes[0].Id }
        };
        db.Activites.AddRange(activites);
        await db.SaveChangesAsync();

        // Participants aux activités
        var participations = new List<ParticipantActivite>();
        // Camp de Pâques : 8 scouts inscrits
        for (int i = 0; i < 8; i++)
            participations.Add(new ParticipantActivite { Id = Guid.NewGuid(), ActiviteId = activites[0].Id, ScoutId = scouts[i].Id, Presence = StatutPresence.Inscrit });
        // Sortie nature (terminée) : 6 scouts, présences variées
        for (int i = 0; i < 6; i++)
            participations.Add(new ParticipantActivite { Id = Guid.NewGuid(), ActiviteId = activites[1].Id, ScoutId = scouts[i].Id, Presence = i < 4 ? StatutPresence.Present : i == 4 ? StatutPresence.Absent : StatutPresence.Excuse });
        // Opération ville propre : 5 scouts
        for (int i = 10; i < 15; i++)
            participations.Add(new ParticipantActivite { Id = Guid.NewGuid(), ActiviteId = activites[5].Id, ScoutId = scouts[i].Id, Presence = StatutPresence.Present });
        db.ParticipantsActivite.AddRange(participations);

        // Commentaires sur activités
        var commentaires = new[]
        {
            new CommentaireActivite { Id = Guid.NewGuid(), ActiviteId = activites[0].Id, AuteurId = gestionnaire.Id, Contenu = "Budget validé par le trésorier. Transport réservé.", TypeAction = "Commentaire" },
            new CommentaireActivite { Id = Guid.NewGuid(), ActiviteId = activites[1].Id, AuteurId = admin.Id, Contenu = "Excellente sortie, les scouts ont beaucoup appris sur la biodiversité locale.", TypeAction = "Clôture" },
            new CommentaireActivite { Id = Guid.NewGuid(), ActiviteId = activites[5].Id, AuteurId = gestionnaire.Id, Contenu = "Bravo à tous les participants ! Le quartier est méconnaissable.", TypeAction = "Clôture" }
        };
        db.CommentairesActivite.AddRange(commentaires);
        await db.SaveChangesAsync();

        // ============================================================
        // 7. COMPÉTENCES (pour plusieurs scouts)
        // ============================================================
        var competences = new[]
        {
            new Competence { Id = Guid.NewGuid(), Nom = "Secourisme PSC1", Description = "Prévention et secours civiques niveau 1", Niveau = "Confirmé", Type = TypeCompetence.Scoute, ScoutId = scouts[0].Id, DateObtention = now.AddMonths(-6) },
            new Competence { Id = Guid.NewGuid(), Nom = "Orientation et cartographie", Niveau = "Intermédiaire", Type = TypeCompetence.Scoute, ScoutId = scouts[0].Id, DateObtention = now.AddMonths(-3) },
            new Competence { Id = Guid.NewGuid(), Nom = "Cuisine en plein air", Niveau = "Débutant", Type = TypeCompetence.Scoute, ScoutId = scouts[1].Id, DateObtention = now.AddMonths(-4) },
            new Competence { Id = Guid.NewGuid(), Nom = "Nœuds et brêlages", Niveau = "Confirmé", Type = TypeCompetence.Scoute, ScoutId = scouts[1].Id, DateObtention = now.AddMonths(-8) },
            new Competence { Id = Guid.NewGuid(), Nom = "Animation de groupe", Niveau = "Avancé", Type = TypeCompetence.Scoute, ScoutId = scouts[4].Id, DateObtention = now.AddMonths(-2) },
            new Competence { Id = Guid.NewGuid(), Nom = "Gestion de projet", Niveau = "Intermédiaire", Type = TypeCompetence.Scoute, ScoutId = scouts[8].Id, DateObtention = now.AddMonths(-5) },
            new Competence { Id = Guid.NewGuid(), Nom = "Communication", Niveau = "Confirmé", Type = TypeCompetence.Autre, ScoutId = scouts[10].Id, DateObtention = now.AddMonths(-1) },
            new Competence { Id = Guid.NewGuid(), Nom = "Informatique de base", Niveau = "Intermédiaire", Type = TypeCompetence.Academique, ScoutId = scouts[12].Id, DateObtention = now.AddMonths(-7) },
            new Competence { Id = Guid.NewGuid(), Nom = "Secourisme PSC1", Niveau = "Confirmé", Type = TypeCompetence.Scoute, ScoutId = scouts[15].Id, DateObtention = now.AddMonths(-4) },
            new Competence { Id = Guid.NewGuid(), Nom = "Vie en plein air", Niveau = "Avancé", Type = TypeCompetence.Scoute, ScoutId = scouts[19].Id, DateObtention = now.AddMonths(-9) }
        };
        db.Competences.AddRange(competences);

        // ============================================================
        // 8. SUIVI ACADÉMIQUE
        // ============================================================
        var suivis = new[]
        {
            new SuiviAcademique { Id = Guid.NewGuid(), ScoutId = scouts[2].Id, AnneeScolaire = "2025-2026", Etablissement = "EPP Cocody Nord", NiveauScolaire = "CM2", Classe = "CM2-A", MoyenneGenerale = 14.5, Mention = "Bien" },
            new SuiviAcademique { Id = Guid.NewGuid(), ScoutId = scouts[3].Id, AnneeScolaire = "2025-2026", Etablissement = "EPP Cocody Nord", NiveauScolaire = "CM1", Classe = "CM1-B", MoyenneGenerale = 12.0, Mention = "Assez Bien" },
            new SuiviAcademique { Id = Guid.NewGuid(), ScoutId = scouts[0].Id, AnneeScolaire = "2025-2026", Etablissement = "Lycée Classique d'Abidjan", NiveauScolaire = "Seconde", Classe = "2nde C", MoyenneGenerale = 15.2, Mention = "Bien" },
            new SuiviAcademique { Id = Guid.NewGuid(), ScoutId = scouts[1].Id, AnneeScolaire = "2025-2026", Etablissement = "Lycée Mamie Faitai", NiveauScolaire = "3ème", Classe = "3ème A", MoyenneGenerale = 13.8, Mention = "Assez Bien" },
            new SuiviAcademique { Id = Guid.NewGuid(), ScoutId = scouts[6].Id, AnneeScolaire = "2025-2026", Etablissement = "Collège Moderne de Marcory", NiveauScolaire = "5ème", Classe = "5ème B", MoyenneGenerale = 11.5, Mention = "Passable" },
            new SuiviAcademique { Id = Guid.NewGuid(), ScoutId = scouts[11].Id, AnneeScolaire = "2025-2026", Etablissement = "EPP Treichville", NiveauScolaire = "CE2", Classe = "CE2-A", MoyenneGenerale = 16.0, Mention = "Très Bien" }
        };
        db.SuivisAcademiques.AddRange(suivis);

        // ============================================================
        // 9. HISTORIQUE DES FONCTIONS
        // ============================================================
        var historiques = new[]
        {
            new HistoriqueFonction { Id = Guid.NewGuid(), Fonction = "Chef de patrouille", DateDebut = now.AddYears(-1), ScoutId = scouts[0].Id, GroupeId = groupes[0].Id, Commentaire = "Nommé chef de patrouille des Aigles" },
            new HistoriqueFonction { Id = Guid.NewGuid(), Fonction = "Louveteau", DateDebut = now.AddYears(-5), DateFin = now.AddYears(-2), ScoutId = scouts[0].Id, GroupeId = groupes[0].Id },
            new HistoriqueFonction { Id = Guid.NewGuid(), Fonction = "Responsable routier", DateDebut = now.AddMonths(-8), ScoutId = scouts[8].Id, GroupeId = groupes[1].Id },
            new HistoriqueFonction { Id = Guid.NewGuid(), Fonction = "ACD", DateDebut = now.AddMonths(-6), ScoutId = scouts[12].Id, GroupeId = groupes[2].Id, Commentaire = "Assistant Commissaire de District" },
            new HistoriqueFonction { Id = Guid.NewGuid(), Fonction = "Gestionnaire", DateDebut = now.AddYears(-2), UserId = gestionnaire.Id, GroupeId = groupes[0].Id, Commentaire = "Responsable administrative du district" }
        };
        db.HistoriqueFonctions.AddRange(historiques);
        await db.SaveChangesAsync();

        // ============================================================
        // 10. TICKETS DE SUPPORT (6 tickets)
        // ============================================================
        DateTime Sla(PrioriteTicket p) => now.AddHours(p switch
        {
            PrioriteTicket.Urgente => 4,
            PrioriteTicket.Haute => 8,
            PrioriteTicket.Normale => 24,
            _ => 72
        });
        var tickets = new[]
        {
            new Ticket { Id = Guid.NewGuid(), NumeroTicket = "INC-20260325-0001", Sujet = "Impossible de modifier mon profil", Description = "Quand je clique sur 'Enregistrer' dans mon profil, rien ne se passe.", Type = TypeTicket.Incident, Categorie = CategorieTicket.Technique, Impact = ImpactTicket.Moyen, Urgence = UrgenceTicket.Haute, Priorite = PrioriteTicket.Haute, Statut = StatutTicket.EnCours, DateLimiteSla = Sla(PrioriteTicket.Haute), CreateurId = scoutUser1.Id, AssigneAId = gestionnaire.Id, DateAffectation = now.AddHours(-6), GroupeAssigneId = groupes[0].Id },
            new Ticket { Id = Guid.NewGuid(), Sujet = "Demande de changement de groupe", Description = "Je souhaite être transféré du groupe Saint-Michel au groupe 1er Abidjan.", Type = TypeTicket.Requete, Categorie = CategorieTicket.Administrative, Priorite = PrioriteTicket.Normale, Statut = StatutTicket.Ouvert, CreateurId = scoutUser2.Id },
            new Ticket { Id = Guid.NewGuid(), Sujet = "Erreur sur mon matricule", Description = "Mon matricule affiché est incorrect. Le bon est 0583753X.", Type = TypeTicket.Incident, Categorie = CategorieTicket.Administrative, Priorite = PrioriteTicket.Haute, Statut = StatutTicket.Resolu, CreateurId = scoutUser1.Id, AssigneAId = admin.Id, DateResolution = now.AddDays(-2), NoteSatisfaction = 5, CommentaireSatisfaction = "Résolu rapidement, merci !" },
            new Ticket { Id = Guid.NewGuid(), Sujet = "Inscription à une activité impossible", Description = "Le bouton d'inscription au camp de Pâques ne fonctionne pas sur mon téléphone.", Type = TypeTicket.Incident, Categorie = CategorieTicket.Activites, Priorite = PrioriteTicket.Normale, Statut = StatutTicket.EnAttente, CreateurId = parentUser.Id, AssigneAId = gestionnaire.Id },
            new Ticket { Id = Guid.NewGuid(), Sujet = "Demande d'adhésion pour mon fils", Description = "Mon fils de 9 ans souhaite rejoindre les louveteaux. Quelle est la procédure ?", Type = TypeTicket.Requete, Categorie = CategorieTicket.Adhesion, Priorite = PrioriteTicket.Basse, Statut = StatutTicket.Resolu, CreateurId = parentUser.Id, DateResolution = now.AddDays(-5), NoteSatisfaction = 4 },
            new Ticket { Id = Guid.NewGuid(), Sujet = "Problème d'accès au tableau de bord", Description = "Je n'arrive pas à voir les statistiques du district.", Type = TypeTicket.Incident, Categorie = CategorieTicket.Technique, Priorite = PrioriteTicket.Urgente, Statut = StatutTicket.Ferme, CreateurId = gestionnaire.Id, AssigneAId = admin.Id, DateResolution = now.AddDays(-1) }
        };
        db.Tickets.AddRange(tickets);
        await db.SaveChangesAsync();

        // Messages sur les tickets
        var messagesTicket = new[]
        {
            new MessageTicket { Id = Guid.NewGuid(), TicketId = tickets[0].Id, AuteurId = scoutUser1.Id, Contenu = "Le problème persiste même après avoir vidé le cache du navigateur." },
            new MessageTicket { Id = Guid.NewGuid(), TicketId = tickets[0].Id, AuteurId = gestionnaire.Id, Contenu = "Merci pour le signalement. Pouvez-vous préciser quel navigateur vous utilisez ?", DateEnvoi = now.AddHours(-2) },
            new MessageTicket { Id = Guid.NewGuid(), TicketId = tickets[2].Id, AuteurId = admin.Id, Contenu = "Le matricule a été corrigé. Veuillez vérifier.", DateEnvoi = now.AddDays(-2) },
            new MessageTicket { Id = Guid.NewGuid(), TicketId = tickets[4].Id, AuteurId = gestionnaire.Id, Contenu = "Bonjour ! Vous pouvez inscrire votre fils en contactant le chef de groupe le plus proche. Les inscriptions sont ouvertes toute l'année.", DateEnvoi = now.AddDays(-5) }
        };
        db.MessagesTicket.AddRange(messagesTicket);

        // Historique des tickets
        var histTickets = new[]
        {
            new HistoriqueTicket { Id = Guid.NewGuid(), TicketId = tickets[0].Id, AncienStatut = StatutTicket.Ouvert, NouveauStatut = StatutTicket.EnCours, AuteurId = gestionnaire.Id, Commentaire = "Pris en charge" },
            new HistoriqueTicket { Id = Guid.NewGuid(), TicketId = tickets[2].Id, AncienStatut = StatutTicket.Ouvert, NouveauStatut = StatutTicket.Resolu, AuteurId = admin.Id, Commentaire = "Matricule corrigé en base" },
            new HistoriqueTicket { Id = Guid.NewGuid(), TicketId = tickets[5].Id, AncienStatut = StatutTicket.Ouvert, NouveauStatut = StatutTicket.Ferme, AuteurId = admin.Id, Commentaire = "Problème de permissions résolu" }
        };
        db.HistoriquesTicket.AddRange(histTickets);
        await db.SaveChangesAsync();

        // ============================================================
        // 11. FINANCES (Transactions variées)
        // ============================================================
        var transactions = new[]
        {
            // Cotisations
            new TransactionFinanciere { Id = Guid.NewGuid(), Libelle = "Cotisation annuelle - Diabaté Moussa", Montant = 15000, Type = TypeTransaction.Recette, Categorie = CategorieFinance.Cotisation, DateTransaction = now.AddMonths(-2), Reference = "COT-2026-001", ScoutId = scouts[0].Id, GroupeId = groupes[0].Id, CreateurId = gestionnaire.Id },
            new TransactionFinanciere { Id = Guid.NewGuid(), Libelle = "Cotisation annuelle - Coulibaly Fatou", Montant = 15000, Type = TypeTransaction.Recette, Categorie = CategorieFinance.Cotisation, DateTransaction = now.AddMonths(-2), Reference = "COT-2026-002", ScoutId = scouts[1].Id, GroupeId = groupes[0].Id, CreateurId = gestionnaire.Id },
            new TransactionFinanciere { Id = Guid.NewGuid(), Libelle = "Cotisation annuelle - Konaté Adama", Montant = 15000, Type = TypeTransaction.Recette, Categorie = CategorieFinance.Cotisation, DateTransaction = now.AddMonths(-1), Reference = "COT-2026-003", ScoutId = scouts[2].Id, GroupeId = groupes[0].Id, CreateurId = gestionnaire.Id },
            // Subventions
            new TransactionFinanciere { Id = Guid.NewGuid(), Libelle = "Subvention Mairie de Cocody", Montant = 500000, Type = TypeTransaction.Recette, Categorie = CategorieFinance.Subvention, DateTransaction = now.AddMonths(-3), Reference = "SUB-2026-001", GroupeId = groupes[0].Id, CreateurId = admin.Id },
            new TransactionFinanciere { Id = Guid.NewGuid(), Libelle = "Don Association des Parents", Montant = 250000, Type = TypeTransaction.Recette, Categorie = CategorieFinance.Don, DateTransaction = now.AddMonths(-1), Reference = "DON-2026-001", CreateurId = admin.Id },
            // Dépenses
            new TransactionFinanciere { Id = Guid.NewGuid(), Libelle = "Achat matériel de camping", Montant = 180000, Type = TypeTransaction.Depense, Categorie = CategorieFinance.Materiel, DateTransaction = now.AddMonths(-1), Reference = "DEP-2026-001", GroupeId = groupes[0].Id, CreateurId = gestionnaire.Id },
            new TransactionFinanciere { Id = Guid.NewGuid(), Libelle = "Transport sortie Bingerville", Montant = 75000, Type = TypeTransaction.Depense, Categorie = CategorieFinance.Transport, DateTransaction = now.AddDays(-10), Reference = "DEP-2026-002", ActiviteId = activites[1].Id, GroupeId = groupes[0].Id, CreateurId = gestionnaire.Id },
            new TransactionFinanciere { Id = Guid.NewGuid(), Libelle = "Alimentation camp de Pâques (avance)", Montant = 300000, Type = TypeTransaction.Depense, Categorie = CategorieFinance.Alimentation, DateTransaction = now.AddDays(-3), Reference = "DEP-2026-003", ActiviteId = activites[0].Id, GroupeId = groupes[0].Id, CreateurId = gestionnaire.Id },
            new TransactionFinanciere { Id = Guid.NewGuid(), Libelle = "Cotisation annuelle - Cissé Abdoulaye", Montant = 15000, Type = TypeTransaction.Recette, Categorie = CategorieFinance.Cotisation, DateTransaction = now.AddMonths(-1), ScoutId = scouts[10].Id, GroupeId = groupes[2].Id, CreateurId = gestionnaire.Id },
            new TransactionFinanciere { Id = Guid.NewGuid(), Libelle = "Fournitures bureau district", Montant = 45000, Type = TypeTransaction.Depense, Categorie = CategorieFinance.Autre, DateTransaction = now.AddDays(-15), Reference = "DEP-2026-004", CreateurId = admin.Id }
        };
        db.TransactionsFinancieres.AddRange(transactions);
        await db.SaveChangesAsync();

        // ============================================================
        // 12. PROJETS AGR (2 projets)
        // ============================================================
        var projetsAGR = new[]
        {
            new ProjetAGR { Id = Guid.NewGuid(), Nom = "Vente de jus de fruits naturels", Description = "Production et vente de jus de bissap, gingembre et baobab lors des événements du district.", Statut = StatutProjetAGR.EnCours, BudgetInitial = 200000, DateDebut = now.AddMonths(-2), Responsable = "Sanogo Drissa", GroupeId = groupes[1].Id, CreateurId = gestionnaire.Id },
            new ProjetAGR { Id = Guid.NewGuid(), Nom = "Atelier de fabrication de savon", Description = "Formation et production de savon artisanal pour financer les activités du groupe.", Statut = StatutProjetAGR.Planifie, BudgetInitial = 350000, DateDebut = now.AddMonths(1), Responsable = "Camara Djénéba", GroupeId = groupes[3].Id, CreateurId = admin.Id }
        };
        db.ProjetsAGR.AddRange(projetsAGR);
        await db.SaveChangesAsync();

        // Transactions AGR
        var transAGR = new[]
        {
            new TransactionFinanciere { Id = Guid.NewGuid(), Libelle = "Achat matières premières (bissap, gingembre)", Montant = 50000, Type = TypeTransaction.Depense, Categorie = CategorieFinance.AGR, DateTransaction = now.AddMonths(-2), ProjetAGRId = projetsAGR[0].Id, CreateurId = gestionnaire.Id },
            new TransactionFinanciere { Id = Guid.NewGuid(), Libelle = "Vente jus - Fête du district", Montant = 120000, Type = TypeTransaction.Recette, Categorie = CategorieFinance.AGR, DateTransaction = now.AddMonths(-1), ProjetAGRId = projetsAGR[0].Id, CreateurId = gestionnaire.Id },
            new TransactionFinanciere { Id = Guid.NewGuid(), Libelle = "Vente jus - Marché de Cocody", Montant = 85000, Type = TypeTransaction.Recette, Categorie = CategorieFinance.AGR, DateTransaction = now.AddDays(-10), ProjetAGRId = projetsAGR[0].Id, CreateurId = gestionnaire.Id }
        };
        db.TransactionsFinancieres.AddRange(transAGR);
        await db.SaveChangesAsync();

        // ============================================================
        // 13. ACTUALITÉS (5 articles)
        // ============================================================
        var actualites = new[]
        {
            new Actualite { Id = Guid.NewGuid(), Titre = "Lancement de la saison scoute 2026", Contenu = "Le district MANGO TAÏKA lance officiellement sa saison scoute 2026 avec de nombreuses activités prévues. Cette année, l'accent sera mis sur le développement durable et la citoyenneté active. Tous les groupes sont invités à participer aux différentes formations et camps organisés tout au long de l'année.", Resume = "Le district lance sa nouvelle saison avec un programme riche en activités.", EstPublie = true, CreateurId = admin.Id, DatePublication = now.AddDays(-20) },
            new Actualite { Id = Guid.NewGuid(), Titre = "Succès de l'opération ville propre à Treichville", Contenu = "Les scouts du groupe Étoile du Sud ont mené une opération de nettoyage exemplaire dans le quartier de Treichville. Plus de 15 scouts ont participé à cette action citoyenne qui a permis de nettoyer les abords du marché et les espaces publics. Le chef de quartier a salué cette initiative.", Resume = "15 scouts mobilisés pour nettoyer Treichville.", EstPublie = true, CreateurId = gestionnaire.Id, DatePublication = now.AddDays(-4) },
            new Actualite { Id = Guid.NewGuid(), Titre = "Formation premiers secours : inscriptions ouvertes", Contenu = "En partenariat avec la Croix-Rouge de Côte d'Ivoire, le district organise une formation PSC1 les 27 et 28 mars 2026. Cette formation est ouverte à tous les scouts de 14 ans et plus. Les places sont limitées à 25 participants.", Resume = "Formation PSC1 en partenariat avec la Croix-Rouge.", EstPublie = true, CreateurId = admin.Id, DatePublication = now.AddDays(-2) },
            new Actualite { Id = Guid.NewGuid(), Titre = "Résultats du concours inter-groupes", Contenu = "Le concours inter-groupes de techniques scoutes s'est tenu le week-end dernier. Le groupe 1er Abidjan remporte la première place, suivi du groupe Étoile du Sud et du groupe Saint-Michel. Félicitations à tous les participants !", Resume = "Le groupe 1er Abidjan remporte le concours.", EstPublie = true, CreateurId = gestionnaire.Id, DatePublication = now.AddDays(-8) },
            new Actualite { Id = Guid.NewGuid(), Titre = "Assemblée générale du district - Convocation", Contenu = "L'assemblée générale annuelle du district MANGO TAÏKA se tiendra le 15 avril 2026 au siège du district. Ordre du jour : bilan moral et financier 2025, programme 2026, élections du bureau.", Resume = "AG annuelle le 15 avril 2026.", EstPublie = false, CreateurId = admin.Id }
        };
        db.Actualites.AddRange(actualites);
        await db.SaveChangesAsync();

        // ============================================================
        // 14. DEMANDES D'AUTORISATION (3 demandes avec suivi)
        // ============================================================
        var demandes = new[]
        {
            new DemandeAutorisation { Id = Guid.NewGuid(), Titre = "Camp de Pâques 2026", Description = "Organisation du camp annuel de Pâques à Grand-Bassam.", TypeActivite = TypeActiviteDemande.Camp, DateActivite = now.AddDays(15), DateFin = now.AddDays(18), Lieu = "Grand-Bassam", NombreParticipants = 30, Objectifs = "Renforcer la cohésion, former aux techniques de camp", MoyensLogistiques = "2 bus, tentes, matériel de cuisine", Budget = "750 000 FCFA", Statut = StatutDemande.Validee, DemandeurId = gestionnaire.Id, ValideurId = admin.Id, DateValidation = now.AddDays(-5), GroupeId = groupes[0].Id, TdrContenu = "TDR Camp de Pâques 2026..." },
            new DemandeAutorisation { Id = Guid.NewGuid(), Titre = "Sortie culturelle au Musée des Civilisations", Description = "Visite éducative au musée pour les louveteaux.", TypeActivite = TypeActiviteDemande.Sortie, DateActivite = now.AddDays(20), Lieu = "Musée des Civilisations, Plateau", NombreParticipants = 15, Objectifs = "Découverte du patrimoine culturel ivoirien", Budget = "100 000 FCFA", Statut = StatutDemande.Soumise, DemandeurId = gestionnaire.Id, GroupeId = groupes[1].Id },
            new DemandeAutorisation { Id = Guid.NewGuid(), Titre = "Cérémonie de la Promesse", Description = "Cérémonie solennelle pour 8 nouveaux louveteaux.", TypeActivite = TypeActiviteDemande.Ceremonie, DateActivite = now.AddDays(25), Lieu = "Paroisse Saint-Michel", NombreParticipants = 40, Statut = StatutDemande.Initialisee, DemandeurId = gestionnaire.Id, GroupeId = groupes[1].Id }
        };
        db.DemandesAutorisation.AddRange(demandes);
        await db.SaveChangesAsync();

        // Suivis des demandes
        var suivisDemande = new[]
        {
            new SuiviDemande { Id = Guid.NewGuid(), DemandeId = demandes[0].Id, AncienStatut = StatutDemande.Initialisee, NouveauStatut = StatutDemande.Initialisee, Commentaire = "Demande créée", Auteur = "Koné Aminata", Date = now.AddDays(-10) },
            new SuiviDemande { Id = Guid.NewGuid(), DemandeId = demandes[0].Id, AncienStatut = StatutDemande.Initialisee, NouveauStatut = StatutDemande.Soumise, Commentaire = "Demande soumise pour validation", Auteur = "Koné Aminata", Date = now.AddDays(-8) },
            new SuiviDemande { Id = Guid.NewGuid(), DemandeId = demandes[0].Id, AncienStatut = StatutDemande.Soumise, NouveauStatut = StatutDemande.Validee, Commentaire = "Demande validée. Bon camp !", Auteur = "Admin MANGO TAÏKA", Date = now.AddDays(-5) },
            new SuiviDemande { Id = Guid.NewGuid(), DemandeId = demandes[1].Id, AncienStatut = StatutDemande.Initialisee, NouveauStatut = StatutDemande.Soumise, Commentaire = "Demande soumise", Auteur = "Koné Aminata", Date = now.AddDays(-3) },
            new SuiviDemande { Id = Guid.NewGuid(), DemandeId = demandes[2].Id, AncienStatut = StatutDemande.Initialisee, NouveauStatut = StatutDemande.Initialisee, Commentaire = "Demande créée", Auteur = "Koné Aminata", Date = now.AddDays(-1) }
        };
        db.SuivisDemande.AddRange(suivisDemande);

        // ============================================================
        // 15. DEMANDES DE GROUPE (3 demandes)
        // ============================================================
        var demandesGroupe = new[]
        {
            new DemandeGroupe { Id = Guid.NewGuid(), NomGroupe = "Groupe Les Étoiles de Bouaké", Commune = "Bouaké", Quartier = "Commerce", NomResponsable = "Konan Yao", TelephoneResponsable = "0708080801", EmailResponsable = "konan.yao@email.ci", Motivation = "Nous souhaitons créer un groupe scout pour encadrer les jeunes de notre quartier.", NombreMembresPrevus = 25, Statut = StatutDemandeGroupe.EnAttente },
            new DemandeGroupe { Id = Guid.NewGuid(), NomGroupe = "Groupe Espoir de Yamoussoukro", Commune = "Yamoussoukro", Quartier = "Habitat", NomResponsable = "Brou Achi", TelephoneResponsable = "0708080802", Motivation = "Ancien scout, je veux transmettre les valeurs scoutes aux jeunes de ma communauté.", NombreMembresPrevus = 20, Statut = StatutDemandeGroupe.Approuvee, TraiteParId = admin.Id, DateTraitement = now.AddDays(-7) },
            new DemandeGroupe { Id = Guid.NewGuid(), NomGroupe = "Groupe Soleil de Daloa", Commune = "Daloa", Quartier = "Lobia", NomResponsable = "Zadi Bi", TelephoneResponsable = "0708080803", Motivation = "Développer le scoutisme dans la région du Haut-Sassandra.", NombreMembresPrevus = 15, Statut = StatutDemandeGroupe.Rejetee, MotifRejet = "Zone déjà couverte par un groupe existant.", TraiteParId = admin.Id, DateTraitement = now.AddDays(-3) }
        };
        db.DemandesGroupe.AddRange(demandesGroupe);
        await db.SaveChangesAsync();

        // ============================================================
        // 16. FORMATIONS LMS (2 formations complètes avec modules, leçons, quiz)
        // ============================================================
        var formation1 = new Formation
        {
            Id = Guid.NewGuid(), Titre = "Les fondamentaux du scoutisme", Description = "Formation d'introduction aux principes, valeurs et techniques de base du scoutisme.", Niveau = NiveauFormation.Debutant, Statut = StatutFormation.Publiee, DureeEstimeeHeures = 6, AuteurId = admin.Id, DatePublication = now.AddDays(-15)
        };
        var formation2 = new Formation
        {
            Id = Guid.NewGuid(), Titre = "Leadership et gestion d'équipe", Description = "Formation avancée pour les chefs de patrouille et responsables d'unité.", Niveau = NiveauFormation.Avance, Statut = StatutFormation.Publiee, DureeEstimeeHeures = 10, AuteurId = admin.Id, BrancheCibleId = branches[1].Id, DatePublication = now.AddDays(-5)
        };
        var formation3 = new Formation
        {
            Id = Guid.NewGuid(), Titre = "Techniques de camp", Description = "Tout savoir pour organiser et vivre un camp scout réussi.", Niveau = NiveauFormation.Intermediaire, Statut = StatutFormation.Brouillon, DureeEstimeeHeures = 8, AuteurId = gestionnaire.Id
        };
        db.Formations.AddRange(formation1, formation2, formation3);
        await db.SaveChangesAsync();

        // Modules formation 1
        var mod1_1 = new ModuleFormation { Id = Guid.NewGuid(), Titre = "Histoire et valeurs du scoutisme", Description = "De Baden-Powell à aujourd'hui", Ordre = 1, FormationId = formation1.Id };
        var mod1_2 = new ModuleFormation { Id = Guid.NewGuid(), Titre = "La Loi et la Promesse scoute", Description = "Comprendre et vivre la Loi scoute", Ordre = 2, FormationId = formation1.Id };
        var mod1_3 = new ModuleFormation { Id = Guid.NewGuid(), Titre = "Techniques de base", Description = "Nœuds, orientation, feu de camp", Ordre = 3, FormationId = formation1.Id };
        // Modules formation 2
        var mod2_1 = new ModuleFormation { Id = Guid.NewGuid(), Titre = "Les styles de leadership", Description = "Découvrir son style de leadership", Ordre = 1, FormationId = formation2.Id };
        var mod2_2 = new ModuleFormation { Id = Guid.NewGuid(), Titre = "Gestion de conflits", Description = "Résoudre les conflits au sein d'une équipe", Ordre = 2, FormationId = formation2.Id };
        db.ModulesFormation.AddRange(mod1_1, mod1_2, mod1_3, mod2_1, mod2_2);
        await db.SaveChangesAsync();

        // Leçons
        var lecons = new[]
        {
            new Lecon { Id = Guid.NewGuid(), Titre = "Baden-Powell et la naissance du scoutisme", Type = TypeLecon.Texte, ContenuTexte = "Robert Baden-Powell fonde le mouvement scout en 1907 après le camp de Brownsea Island en Angleterre. Le scoutisme arrive en Côte d'Ivoire dans les années 1930 et se développe rapidement après l'indépendance.", Ordre = 1, DureeMinutes = 15, ModuleId = mod1_1.Id },
            new Lecon { Id = Guid.NewGuid(), Titre = "Les valeurs fondamentales", Type = TypeLecon.Texte, ContenuTexte = "Le scoutisme repose sur trois piliers : le développement physique, intellectuel et spirituel. Les valeurs de service, de fraternité et de respect de la nature guident chaque scout.", Ordre = 2, DureeMinutes = 10, ModuleId = mod1_1.Id },
            new Lecon { Id = Guid.NewGuid(), Titre = "La Loi scoute expliquée", Type = TypeLecon.Texte, ContenuTexte = "La Loi scoute comprend 10 articles qui définissent le code de conduite du scout. Chaque article est un engagement personnel envers soi-même et la communauté.", Ordre = 1, DureeMinutes = 20, ModuleId = mod1_2.Id },
            new Lecon { Id = Guid.NewGuid(), Titre = "La cérémonie de la Promesse", Type = TypeLecon.Texte, ContenuTexte = "La Promesse est un moment solennel où le scout s'engage à respecter la Loi scoute. Elle se déroule généralement lors d'une cérémonie en présence de tout le groupe.", Ordre = 2, DureeMinutes = 10, ModuleId = mod1_2.Id },
            new Lecon { Id = Guid.NewGuid(), Titre = "Les nœuds essentiels", Type = TypeLecon.Texte, ContenuTexte = "Tout scout doit maîtriser les nœuds de base : nœud plat, nœud de chaise, nœud de cabestan, nœud de huit. Chaque nœud a une utilisation spécifique.", Ordre = 1, DureeMinutes = 25, ModuleId = mod1_3.Id },
            new Lecon { Id = Guid.NewGuid(), Titre = "S'orienter avec une boussole", Type = TypeLecon.Texte, ContenuTexte = "L'orientation est une compétence fondamentale. Apprenez à lire une carte topographique et à utiliser une boussole pour vous repérer en milieu naturel.", Ordre = 2, DureeMinutes = 20, ModuleId = mod1_3.Id },
            new Lecon { Id = Guid.NewGuid(), Titre = "Leadership situationnel", Type = TypeLecon.Texte, ContenuTexte = "Le leadership situationnel consiste à adapter son style de management en fonction de la maturité et des compétences de son équipe.", Ordre = 1, DureeMinutes = 30, ModuleId = mod2_1.Id },
            new Lecon { Id = Guid.NewGuid(), Titre = "Techniques de médiation", Type = TypeLecon.Texte, ContenuTexte = "La médiation est un processus structuré pour résoudre les conflits. Écoute active, reformulation et recherche de solutions gagnant-gagnant.", Ordre = 1, DureeMinutes = 25, ModuleId = mod2_2.Id }
        };
        db.Lecons.AddRange(lecons);
        await db.SaveChangesAsync();

        // Quiz
        var quiz1 = new Quiz { Id = Guid.NewGuid(), Titre = "Quiz : Histoire du scoutisme", NoteMinimale = 70, ModuleId = mod1_1.Id };
        var quiz2 = new Quiz { Id = Guid.NewGuid(), Titre = "Quiz : La Loi scoute", NoteMinimale = 80, ModuleId = mod1_2.Id };
        db.Quizzes.AddRange(quiz1, quiz2);
        await db.SaveChangesAsync();

        // Questions et réponses
        var q1 = new QuestionQuiz { Id = Guid.NewGuid(), Enonce = "En quelle année Baden-Powell a-t-il fondé le mouvement scout ?", Ordre = 1, QuizId = quiz1.Id };
        var q2 = new QuestionQuiz { Id = Guid.NewGuid(), Enonce = "Où s'est tenu le premier camp scout ?", Ordre = 2, QuizId = quiz1.Id };
        var q3 = new QuestionQuiz { Id = Guid.NewGuid(), Enonce = "Combien d'articles comporte la Loi scoute ?", Ordre = 1, QuizId = quiz2.Id };
        db.QuestionsQuiz.AddRange(q1, q2, q3);
        await db.SaveChangesAsync();

        var reponses = new[]
        {
            new ReponseQuiz { Id = Guid.NewGuid(), Texte = "1905", EstCorrecte = false, Ordre = 1, QuestionId = q1.Id },
            new ReponseQuiz { Id = Guid.NewGuid(), Texte = "1907", EstCorrecte = true, Ordre = 2, QuestionId = q1.Id },
            new ReponseQuiz { Id = Guid.NewGuid(), Texte = "1910", EstCorrecte = false, Ordre = 3, QuestionId = q1.Id },
            new ReponseQuiz { Id = Guid.NewGuid(), Texte = "1900", EstCorrecte = false, Ordre = 4, QuestionId = q1.Id },
            new ReponseQuiz { Id = Guid.NewGuid(), Texte = "Brownsea Island", EstCorrecte = true, Ordre = 1, QuestionId = q2.Id },
            new ReponseQuiz { Id = Guid.NewGuid(), Texte = "Hyde Park", EstCorrecte = false, Ordre = 2, QuestionId = q2.Id },
            new ReponseQuiz { Id = Guid.NewGuid(), Texte = "Gilwell Park", EstCorrecte = false, Ordre = 3, QuestionId = q2.Id },
            new ReponseQuiz { Id = Guid.NewGuid(), Texte = "8", EstCorrecte = false, Ordre = 1, QuestionId = q3.Id },
            new ReponseQuiz { Id = Guid.NewGuid(), Texte = "10", EstCorrecte = true, Ordre = 2, QuestionId = q3.Id },
            new ReponseQuiz { Id = Guid.NewGuid(), Texte = "12", EstCorrecte = false, Ordre = 3, QuestionId = q3.Id }
        };
        db.ReponsesQuiz.AddRange(reponses);
        await db.SaveChangesAsync();

        // Inscriptions aux formations
        var inscriptions = new[]
        {
            new InscriptionFormation { Id = Guid.NewGuid(), ScoutId = scouts[0].Id, FormationId = formation1.Id, Statut = StatutInscription.Terminee, ProgressionPourcent = 100, DateTerminee = now.AddDays(-3) },
            new InscriptionFormation { Id = Guid.NewGuid(), ScoutId = scouts[1].Id, FormationId = formation1.Id, Statut = StatutInscription.EnCours, ProgressionPourcent = 60 },
            new InscriptionFormation { Id = Guid.NewGuid(), ScoutId = scouts[4].Id, FormationId = formation1.Id, Statut = StatutInscription.EnCours, ProgressionPourcent = 30 },
            new InscriptionFormation { Id = Guid.NewGuid(), ScoutId = scouts[0].Id, FormationId = formation2.Id, Statut = StatutInscription.EnCours, ProgressionPourcent = 40 },
            new InscriptionFormation { Id = Guid.NewGuid(), ScoutId = scouts[10].Id, FormationId = formation2.Id, Statut = StatutInscription.EnCours, ProgressionPourcent = 10 }
        };
        db.InscriptionsFormation.AddRange(inscriptions);

        // Progressions de leçons
        var progressions = new[]
        {
            new ProgressionLecon { Id = Guid.NewGuid(), ScoutId = scouts[0].Id, LeconId = lecons[0].Id, EstTerminee = true, DateTerminee = now.AddDays(-10) },
            new ProgressionLecon { Id = Guid.NewGuid(), ScoutId = scouts[0].Id, LeconId = lecons[1].Id, EstTerminee = true, DateTerminee = now.AddDays(-9) },
            new ProgressionLecon { Id = Guid.NewGuid(), ScoutId = scouts[0].Id, LeconId = lecons[2].Id, EstTerminee = true, DateTerminee = now.AddDays(-8) },
            new ProgressionLecon { Id = Guid.NewGuid(), ScoutId = scouts[0].Id, LeconId = lecons[3].Id, EstTerminee = true, DateTerminee = now.AddDays(-7) },
            new ProgressionLecon { Id = Guid.NewGuid(), ScoutId = scouts[0].Id, LeconId = lecons[4].Id, EstTerminee = true, DateTerminee = now.AddDays(-5) },
            new ProgressionLecon { Id = Guid.NewGuid(), ScoutId = scouts[0].Id, LeconId = lecons[5].Id, EstTerminee = true, DateTerminee = now.AddDays(-4) },
            new ProgressionLecon { Id = Guid.NewGuid(), ScoutId = scouts[1].Id, LeconId = lecons[0].Id, EstTerminee = true, DateTerminee = now.AddDays(-6) },
            new ProgressionLecon { Id = Guid.NewGuid(), ScoutId = scouts[1].Id, LeconId = lecons[1].Id, EstTerminee = true, DateTerminee = now.AddDays(-5) },
            new ProgressionLecon { Id = Guid.NewGuid(), ScoutId = scouts[1].Id, LeconId = lecons[2].Id, EstTerminee = true, DateTerminee = now.AddDays(-4) }
        };
        db.ProgressionsLecon.AddRange(progressions);

        // Tentatives de quiz
        var tentatives = new[]
        {
            new TentativeQuiz { Id = Guid.NewGuid(), ScoutId = scouts[0].Id, QuizId = quiz1.Id, Score = 100, Reussi = true, DateTentative = now.AddDays(-8) },
            new TentativeQuiz { Id = Guid.NewGuid(), ScoutId = scouts[0].Id, QuizId = quiz2.Id, Score = 100, Reussi = true, DateTentative = now.AddDays(-6) },
            new TentativeQuiz { Id = Guid.NewGuid(), ScoutId = scouts[1].Id, QuizId = quiz1.Id, Score = 50, Reussi = false, DateTentative = now.AddDays(-5) },
            new TentativeQuiz { Id = Guid.NewGuid(), ScoutId = scouts[1].Id, QuizId = quiz1.Id, Score = 100, Reussi = true, DateTentative = now.AddDays(-4) }
        };
        db.TentativesQuiz.AddRange(tentatives);
        await db.SaveChangesAsync();

        // ============================================================
        // 17. MOT DU COMMISSAIRE
        // ============================================================
        var mots = new[]
        {
            new MotCommissaire { Id = Guid.NewGuid(), Contenu = "Chers scouts, chères scoutes, l'année 2026 s'annonce riche en défis et en opportunités. Notre district continue de grandir et de rayonner grâce à votre engagement quotidien. Ensemble, poursuivons notre mission de former des citoyens responsables et solidaires. Toujours prêts !", Annee = 2026, EstActif = true },
            new MotCommissaire { Id = Guid.NewGuid(), Contenu = "L'année 2025 a été marquée par de belles réalisations : 3 nouveaux groupes créés, plus de 50 scouts formés et une présence renforcée dans nos communautés. Merci à tous pour votre dévouement.", Annee = 2025, EstActif = false }
        };
        db.MotsCommissaire.AddRange(mots);

        // ============================================================
        // 18. MEMBRES HISTORIQUES
        // ============================================================
        var membresHist = new[]
        {
            new MembreHistorique { Id = Guid.NewGuid(), Nom = "Konan Kouadio Jean", Description = "Premier Commissaire de District, fondateur du mouvement scout dans la région.", Periode = "1985-1995", Categories = CategorieHistorique.AncienCommissaire, Ordre = 1 },
            new MembreHistorique { Id = Guid.NewGuid(), Nom = "Traoré Mamadou", Description = "A développé le scoutisme dans les zones rurales du district.", Periode = "1995-2005", Categories = CategorieHistorique.AncienCommissaire, Ordre = 2 },
            new MembreHistorique { Id = Guid.NewGuid(), Nom = "Yao Kouassi Pierre", Description = "A modernisé les méthodes de formation et créé le premier camp permanent.", Periode = "2005-2015", Categories = CategorieHistorique.AncienCommissaire, Ordre = 3 },
            new MembreHistorique { Id = Guid.NewGuid(), Nom = "Diallo Ibrahima", Description = "Chef du groupe 1er Abidjan pendant 10 ans.", Periode = "2000-2010", Categories = CategorieHistorique.AncienChefGroupe, Ordre = 1 },
            new MembreHistorique { Id = Guid.NewGuid(), Nom = "Bamba Aïcha", Description = "Première femme chef de groupe du district.", Periode = "2010-2018", Categories = CategorieHistorique.AncienChefGroupe, Ordre = 2 },
            new MembreHistorique { Id = Guid.NewGuid(), Nom = "Dr. Koné Seydou", Description = "Médecin et membre fondateur du Conseil d'Administration du District.", Periode = "1990-2010", Categories = CategorieHistorique.MembreCAD, Ordre = 1 },
            new MembreHistorique { Id = Guid.NewGuid(), Nom = "Mme Ouattara Fanta", Description = "Trésorière du CAD, a structuré la gestion financière du district.", Periode = "2005-2020", Categories = CategorieHistorique.MembreCAD, Ordre = 2 }
        };
        db.MembresHistoriques.AddRange(membresHist);

        // ============================================================
        // 19. PARTENAIRES ET RÉSEAUX SOCIAUX
        // ============================================================
        var partenaires = new[]
        {
            new Partenaire { Id = Guid.NewGuid(), Nom = "ASCCI", Description = "Association du Scoutisme et du Guidisme de Côte d'Ivoire", SiteWeb = "https://www.ascci.ci", TypePartenariat = "Institutionnel", Ordre = 1 },
            new Partenaire { Id = Guid.NewGuid(), Nom = "Croix-Rouge de Côte d'Ivoire", Description = "Partenaire pour les formations premiers secours", SiteWeb = "https://www.croixrouge.ci", TypePartenariat = "Formation", Ordre = 2 },
            new Partenaire { Id = Guid.NewGuid(), Nom = "Mairie de Cocody", Description = "Soutien logistique et financier pour les activités communautaires", TypePartenariat = "Institutionnel", Ordre = 3 },
            new Partenaire { Id = Guid.NewGuid(), Nom = "Orange Côte d'Ivoire", Description = "Sponsor principal du camp annuel", SiteWeb = "https://www.orange.ci", TypePartenariat = "Sponsor", Ordre = 4 }
        };
        db.Partenaires.AddRange(partenaires);

        var liens = new[]
        {
            new LienReseauSocial { Id = Guid.NewGuid(), Plateforme = "Facebook", Url = "https://www.facebook.com/share/1BAtkMx8sd/", Icone = "bi-facebook", Ordre = 1 },
            new LienReseauSocial { Id = Guid.NewGuid(), Plateforme = "Instagram", Url = "https://www.instagram.com/scouts_du_darp?igsh=ajc0eDB5ODNtaWk0", Icone = "bi-instagram", Ordre = 2 },
            new LienReseauSocial { Id = Guid.NewGuid(), Plateforme = "WhatsApp", Url = "/Home/WhatsApp", Icone = "bi-whatsapp", Ordre = 3 },
            new LienReseauSocial { Id = Guid.NewGuid(), Plateforme = "TikTok", Url = "https://www.tiktok.com/@scoutsmangotaika?_r=1&_t=ZS-94zhRDuRtOW", Icone = "bi-tiktok", Ordre = 4 }
        };
        db.LiensReseauxSociaux.AddRange(liens);

        // ============================================================
        // 20. GALERIE MÉDIA
        // ============================================================
        var galeries = new[]
        {
            new Galerie { Id = Guid.NewGuid(), Titre = "Camp de Noël 2025", Description = "Photos du camp de Noël à Assinie", CheminMedia = "/uploads/galerie/camp-noel-2025.jpg", TypeMedia = "image", EstPublie = true, DateUpload = now.AddMonths(-3) },
            new Galerie { Id = Guid.NewGuid(), Titre = "Cérémonie de la Promesse", Description = "Cérémonie solennelle des nouveaux éclaireurs", CheminMedia = "/uploads/galerie/promesse-2025.jpg", TypeMedia = "image", EstPublie = true, DateUpload = now.AddMonths(-2) },
            new Galerie { Id = Guid.NewGuid(), Titre = "Opération ville propre", Description = "Les scouts en action à Treichville", CheminMedia = "/uploads/galerie/ville-propre.jpg", TypeMedia = "image", EstPublie = true, DateUpload = now.AddDays(-5) },
            new Galerie { Id = Guid.NewGuid(), Titre = "Feu de camp - Veillée", Description = "Veillée autour du feu de camp", CheminMedia = "/uploads/galerie/feu-camp.jpg", TypeMedia = "image", EstPublie = true, DateUpload = now.AddMonths(-1) },
            new Galerie { Id = Guid.NewGuid(), Titre = "Hymne scout - Vidéo", Description = "Les scouts chantent l'hymne du district", CheminMedia = "/uploads/galerie/hymne-scout.mp4", TypeMedia = "video", EstPublie = false, DateUpload = now.AddDays(-2) }
        };
        db.Galeries.AddRange(galeries);

        // ============================================================
        // 21. MESSAGES DE CONTACT ET LIVRE D'OR
        // ============================================================
        var contacts = new[]
        {
            new ContactMessage { Id = Guid.NewGuid(), Nom = "Konan Marie", Email = "marie.konan@email.ci", Sujet = "Inscription de mon enfant", Message = "Bonjour, je souhaite inscrire mon fils de 10 ans dans un groupe scout près de Cocody. Pouvez-vous m'orienter ?", Type = "Contact", DateEnvoi = now.AddDays(-3) },
            new ContactMessage { Id = Guid.NewGuid(), Nom = "Brou Achi", Email = "achi.brou@email.ci", Sujet = "Partenariat entreprise", Message = "Notre entreprise souhaite soutenir les activités du district. Comment pouvons-nous collaborer ?", Type = "Contact", EstLu = true, DateEnvoi = now.AddDays(-7) },
            new ContactMessage { Id = Guid.NewGuid(), Nom = "Ancien scout", Email = "ancien@email.ci", Sujet = "Bravo pour le travail", Message = "En tant qu'ancien scout du district, je suis fier de voir le travail accompli. Continuez ainsi !", Type = "Avis", DateEnvoi = now.AddDays(-5) }
        };
        db.ContactMessages.AddRange(contacts);

        var livreDor = new[]
        {
            new LivreDor { Id = Guid.NewGuid(), NomAuteur = "Famille Diabaté", Message = "Merci au district MANGO TAÏKA pour l'encadrement de notre fils. Il a beaucoup grandi grâce au scoutisme.", EstValide = true, DateValidation = now.AddDays(-10) },
            new LivreDor { Id = Guid.NewGuid(), NomAuteur = "Chef Konan", Message = "Le scoutisme forme des hommes et des femmes de valeur. Fier d'appartenir à ce mouvement.", EstValide = true, DateValidation = now.AddDays(-8) },
            new LivreDor { Id = Guid.NewGuid(), NomAuteur = "Visiteur anonyme", Message = "Belle plateforme ! Bravo pour la modernisation.", EstValide = false }
        };
        db.LivreDor.AddRange(livreDor);

        // ============================================================
        // 22. CODES D'INVITATION
        // ============================================================
        var codes = new[]
        {
            new CodeInvitation { Id = Guid.NewGuid(), Code = "MT-20260301-A1B2C3", CreateurId = admin.Id, EstUtilise = true, DateUtilisation = now.AddDays(-15), UtilisePaId = gestionnaire.Id },
            new CodeInvitation { Id = Guid.NewGuid(), Code = "MT-20260315-D4E5F6", CreateurId = admin.Id, EstUtilise = false },
            new CodeInvitation { Id = Guid.NewGuid(), Code = "MT-20260320-G7H8I9", CreateurId = admin.Id, EstUtilise = false }
        };
        db.CodesInvitation.AddRange(codes);

        await db.SaveChangesAsync();
    }

    private static async Task<ApplicationUser> CreateUser(UserManager<ApplicationUser> userManager, string phone, string nom, string prenom, string email, string role)
    {
        var user = new ApplicationUser
        {
            UserName = phone,
            PhoneNumber = phone,
            Email = email,
            Nom = nom,
            Prenom = prenom,
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
            IsActive = true
        };
        await userManager.CreateAsync(user, "User@2026!");
        await userManager.AddToRoleAsync(user, role);
        return user;
    }
}
