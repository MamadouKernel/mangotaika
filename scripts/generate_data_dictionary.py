from __future__ import annotations

import re
from dataclasses import dataclass
from datetime import date
from pathlib import Path

ROOT = Path(r"C:\Users\kerne\Downloads\rodi\new\MangoTaika")
ENTITIES = ROOT / "Data" / "Entities"
APPDB = ROOT / "Data" / "AppDbContext.cs"
OUTPUT = ROOT / "docs" / "dictionnaire-donnees.md"

SCALAR_TYPES = {
    "string", "string?", "int", "int?", "bool", "bool?", "Guid", "Guid?", "DateTime", "DateTime?",
    "decimal", "decimal?", "double", "double?", "long", "long?", "float", "float?", "byte[]", "byte[]?"
}

DOMAIN_ORDER = [
    "Territoire, identites et parcours scout",
    "Activites, gouvernance et workflows",
    "Support, finance et cotisations",
    "LMS / formation",
    "Communication et vitrine publique",
    "Tables techniques Identity",
]

DOMAIN_TABLES = {
    "Territoire, identites et parcours scout": [
        "AspNetUsers", "Groupes", "Branches", "Scouts", "Parents", "ParentScout", "Competences",
        "HistoriqueFonctions", "SuivisAcademiques", "CodesInvitation", "EtapesParcoursScouts",
        "InscriptionsAnnuellesScouts",
    ],
    "Activites, gouvernance et workflows": [
        "Activites", "DocumentsActivite", "ParticipantsActivite", "CommentairesActivite", "DemandesAutorisation",
        "SuivisDemande", "DemandesGroupe", "ProgrammesAnnuels", "RapportsActivite", "PropositionsMaitriseAnnuelles",
    ],
    "Support, finance et cotisations": [
        "SupportCatalogueServices", "SupportKnowledgeArticles", "Tickets", "MessagesTicket", "TicketPiecesJointes",
        "HistoriquesTicket", "NotificationsUtilisateur", "TransactionsFinancieres", "ProjetsAGR",
        "CotisationsNationalesImports", "CotisationsNationalesImportLignes",
    ],
    "LMS / formation": [
        "Formations", "FormationsPrerequis", "ModulesFormation", "Lecons", "Quizzes", "QuestionsQuiz",
        "ReponsesQuiz", "SessionsFormation", "InscriptionsFormation", "ProgressionsLecon", "TentativesQuiz",
        "CertificationsFormation", "JalonsFormation", "AnnoncesFormation", "DiscussionsFormation",
        "MessagesDiscussionFormation",
    ],
    "Communication et vitrine publique": [
        "Actualites", "Galeries", "MotsCommissaire", "LivreDor", "ContactMessages", "MembresHistoriques",
        "Partenaires", "LiensReseauxSociaux",
    ],
    "Tables techniques Identity": [
        "AspNetRoles", "AspNetUserRoles", "AspNetUserClaims", "AspNetUserLogins", "AspNetUserTokens", "AspNetRoleClaims",
    ],
}

TABLE_PURPOSES = {
    "AspNetUsers": "Comptes applicatifs et administratifs, avec extensions metier MangoTaika.",
    "Groupes": "Entites scoutes du district, y compris l'equipe de district.",
    "Branches": "Unites ou branches d'age rattachees a un groupe.",
    "Scouts": "Fiches centrales des membres scouts et responsables terrain.",
    "Parents": "Representants ou contacts parentaux relies aux scouts.",
    "ParentScout": "Table de jointure many-to-many entre parents et scouts.",
    "Competences": "Competences et acquis du scout.",
    "HistoriqueFonctions": "Historisation des fonctions exercees par un scout ou un utilisateur.",
    "SuivisAcademiques": "Suivi scolaire et academique du scout.",
    "CodesInvitation": "Codes d'invitation utilises pour l'inscription ou l'activation de comptes.",
    "EtapesParcoursScouts": "Etapes du parcours scout, avec prevision et validation.",
    "InscriptionsAnnuellesScouts": "Historique annuel des inscriptions et de la conformite du scout.",
    "Activites": "Activites scouts soumises, suivies et validees.",
    "DocumentsActivite": "Documents rattaches a une activite.",
    "ParticipantsActivite": "Participants scouts inscrits a une activite.",
    "CommentairesActivite": "Commentaires, actions et journal d'une activite.",
    "DemandesAutorisation": "Demandes administratives ou autorisations d'activite.",
    "SuivisDemande": "Journal de suivi associe a une demande d'autorisation.",
    "DemandesGroupe": "Demandes de creation / reconnaissance d'entite scoute.",
    "ProgrammesAnnuels": "Programme annuel d'un groupe ou du district.",
    "RapportsActivite": "Rapports post-activite avec validation.",
    "PropositionsMaitriseAnnuelles": "Propositions annuelles de maitrise / encadrement.",
    "SupportCatalogueServices": "Catalogue des services du centre de support.",
    "SupportKnowledgeArticles": "Base de connaissances du support.",
    "Tickets": "Tickets du centre de support avec SLA, escalade et satisfaction.",
    "MessagesTicket": "Messages d'echange sur un ticket.",
    "TicketPiecesJointes": "Pieces jointes attachees a un ticket.",
    "HistoriquesTicket": "Historique des changements d'un ticket.",
    "NotificationsUtilisateur": "Notifications internes recues par les utilisateurs.",
    "TransactionsFinancieres": "Ecritures financieres liees aux scouts, activites, groupes ou projets.",
    "ProjetsAGR": "Projets d'activites generatrices de revenus.",
    "CotisationsNationalesImports": "Lots d'import des cotisations nationales.",
    "CotisationsNationalesImportLignes": "Lignes detaillees d'un import de cotisations nationales.",
    "Formations": "Cours ou parcours de formation du LMS.",
    "FormationsPrerequis": "Relations de prerequis entre formations.",
    "ModulesFormation": "Modules composant une formation.",
    "Lecons": "Lecons appartenant a un module de formation.",
    "Quizzes": "Quiz rattaches a un module de formation.",
    "QuestionsQuiz": "Questions d'un quiz.",
    "ReponsesQuiz": "Reponses possibles a une question de quiz.",
    "SessionsFormation": "Sessions publiees d'une formation.",
    "InscriptionsFormation": "Inscriptions des scouts aux formations.",
    "ProgressionsLecon": "Progression d'un scout sur une lecon.",
    "TentativesQuiz": "Tentatives d'un scout sur un quiz.",
    "CertificationsFormation": "Certificats, attestations ou badges emis apres formation.",
    "JalonsFormation": "Dates clefs et jalons d'une formation.",
    "AnnoncesFormation": "Annonces publiees dans le contexte d'une formation.",
    "DiscussionsFormation": "Discussions ou fils de forum associes a une formation.",
    "MessagesDiscussionFormation": "Messages postes dans une discussion de formation.",
    "Actualites": "Actualites publiees sur le portail.",
    "Galeries": "Elements medias de la galerie.",
    "MotsCommissaire": "Mot du commissaire publie sur la plateforme.",
    "LivreDor": "Messages du livre d'or.",
    "ContactMessages": "Messages envoyes depuis le formulaire de contact.",
    "MembresHistoriques": "Membres historiques ou anciens responsables mis en avant.",
    "Partenaires": "Partenaires institutionnels ou prives.",
    "LiensReseauxSociaux": "Liens de reseaux sociaux exposes sur la plateforme.",
    "AspNetRoles": "Roles techniques ASP.NET Identity.",
    "AspNetUserRoles": "Jointure utilisateur-role Identity.",
    "AspNetUserClaims": "Claims utilises par Identity.",
    "AspNetUserLogins": "Logins externes Identity.",
    "AspNetUserTokens": "Tokens techniques Identity.",
    "AspNetRoleClaims": "Claims attaches aux roles Identity.",
}

TABLE_NOTES = {
    "Groupes": ["NomNormalise unique sur les groupes actifs."],
    "Branches": ["Unicite logique du nom dans un groupe actif via (GroupeId, NomNormalise)."],
    "Scouts": ["Matricule unique.", "NumeroCarte unique si renseigne."],
    "CodesInvitation": ["Code unique."],
    "ParticipantsActivite": ["Unicite (ActiviteId, ScoutId)."],
    "InscriptionsAnnuellesScouts": ["Unicite (ScoutId, AnneeReference)."],
    "ProgrammesAnnuels": ["Unicite (GroupeId, AnneeReference)."],
    "RapportsActivite": ["Un rapport maximum par activite."],
    "PropositionsMaitriseAnnuelles": ["Unicite (GroupeId, AnneeReference)."],
    "CotisationsNationalesImportLignes": ["Index sur (ImportId, Matricule)."],
    "SupportCatalogueServices": ["Code unique."],
    "InscriptionsFormation": ["Unicite (ScoutId, FormationId)."],
    "ProgressionsLecon": ["Unicite (ScoutId, LeconId)."],
    "CertificationsFormation": ["Code unique.", "Unicite (ScoutId, FormationId, Type)."],
}

FK_TARGETS = {
    "UserId": "AspNetUsers",
    "CreateurId": "AspNetUsers",
    "ValideurId": "AspNetUsers",
    "ValideParId": "AspNetUsers",
    "AuteurId": "AspNetUsers",
    "DemandeurId": "AspNetUsers",
    "TraiteParId": "AspNetUsers",
    "AssigneAId": "AspNetUsers",
    "AjouteParId": "AspNetUsers",
    "AssigneParDefautId": "AspNetUsers",
    "UtilisePaId": "AspNetUsers",
    "ResponsableId": "AspNetUsers",
    "GroupeParDefautId": "Groupes",
    "GroupeId": "Groupes",
    "BrancheId": "Branches",
    "ChefUniteId": "Scouts",
    "ChefGroupeScoutId": "Scouts",
    "ScoutId": "Scouts",
    "ParentId": "Parents",
    "ActiviteId": "Activites",
    "DemandeId": "DemandesAutorisation",
    "TicketId": "Tickets",
    "ServiceCatalogueId": "SupportCatalogueServices",
    "ProjetAGRId": "ProjetsAGR",
    "FormationId": "Formations",
    "PrerequisFormationId": "Formations",
    "ModuleId": "ModulesFormation",
    "LeconId": "Lecons",
    "QuizId": "Quizzes",
    "QuestionId": "QuestionsQuiz",
    "SessionFormationId": "SessionsFormation",
    "InscriptionFormationId": "InscriptionsFormation",
    "DiscussionFormationId": "DiscussionsFormation",
    "ImportId": "CotisationsNationalesImports",
    "CompetenceLieeId": "Competences",
}

@dataclass
class Field:
    name: str
    type_name: str
    nullable: bool
    role: str


def parse_dbsets() -> dict[str, str]:
    text = APPDB.read_text(encoding="utf-8")
    mapping: dict[str, str] = {}
    for match in re.finditer(r"public\s+DbSet<(?P<class>\w+)>\s+(?P<table>\w+)\s*=>\s*Set<", text):
        mapping[match.group("class")] = match.group("table")
    mapping["ApplicationUser"] = "AspNetUsers"
    return mapping


def parse_entities() -> tuple[dict[str, list[Field]], set[str], dict[str, str]]:
    class_fields: dict[str, list[Field]] = {}
    enum_names: set[str] = set()
    class_files: dict[str, str] = {}

    for path in sorted(ENTITIES.glob("*.cs")):
        lines = path.read_text(encoding="utf-8").splitlines()
        for line in lines:
            enum_match = re.match(r"\s*public\s+enum\s+(\w+)", line)
            if enum_match:
                enum_names.add(enum_match.group(1))

        current_class = None
        brace_depth = 0
        for line in lines:
            class_match = re.match(r"\s*public\s+class\s+(\w+)", line)
            if class_match:
                current_class = class_match.group(1)
                class_files[current_class] = str(path.relative_to(ROOT)).replace("\\", "/")
                class_fields.setdefault(current_class, [])
                brace_depth = line.count("{") - line.count("}")
                continue

            if current_class is None:
                continue

            brace_depth += line.count("{") - line.count("}")
            prop_match = re.match(r"\s*public\s+([A-Za-z0-9_<>,\?\[\]\.]+)\s+(\w+)\s*\{\s*get;\s*(?:private\s+)?(?:set|init);\s*\}", line)
            if prop_match:
                type_name = prop_match.group(1)
                field_name = prop_match.group(2)
                if type_name.startswith("ICollection<") or type_name.startswith("List<"):
                    pass
                else:
                    simple_type = type_name.split(".")[-1]
                    if simple_type in SCALAR_TYPES or simple_type in enum_names or field_name == "Id" or field_name.endswith("Id"):
                        class_fields[current_class].append(Field(
                            name=field_name,
                            type_name=simple_type,
                            nullable=simple_type.endswith("?"),
                            role=classify_role(field_name, simple_type, enum_names),
                        ))
            if brace_depth <= 0:
                current_class = None

    return class_fields, enum_names, class_files


def classify_role(field_name: str, type_name: str, enum_names: set[str]) -> str:
    if field_name == "Id":
        return "PK"
    if field_name.endswith("Id"):
        return "FK"
    if type_name in enum_names:
        return "Enum"
    if field_name.startswith("Date"):
        return "Date"
    if field_name.startswith("Est") or field_name.startswith("Is"):
        return "Flag"
    if field_name == "Statut":
        return "Statut"
    return "Metier"


def format_nullable(field: Field) -> str:
    return "Oui" if field.nullable else "Non"


def describe_field(field: Field) -> str:
    if field.name == "Id":
        return "Identifiant technique unique de l'enregistrement."
    if field.role == "FK":
        target = FK_TARGETS.get(field.name, field.name[:-2])
        prefix = "Reference vers" if field.name != "CompetenceLieeId" else "Reference logique vers"
        return f"{prefix} `{target}`."
    if field.name in {"Nom", "Titre", "LibelleAnnee", "Code", "Reference"}:
        return "Libelle ou identifiant metier lisible."
    if field.name.startswith("Date"):
        return "Date ou horodatage du processus metier."
    if field.name.startswith("Est") or field.name.startswith("Is"):
        return "Indicateur booleen de statut ou de visibilite."
    if field.name == "Statut":
        return "Statut fonctionnel de l'enregistrement."
    if field.name in {"Description", "Contenu", "Message", "Commentaire", "Observations", "ObservationsComplementaires", "MotsCles", "Resume", "ResumeExecutif"}:
        return "Contenu textuel ou descriptif."
    if field.name.endswith("Url") or "Chemin" in field.name:
        return "Chemin, URL ou ressource associee."
    if field.name.startswith("Nombre") or field.name.endswith("Pourcent") or field.name.endswith("Score") or field.name == "Montant":
        return "Valeur numerique de suivi ou de calcul."
    return "Champ metier de la table."


def write_section(lines: list[str], table: str, fields: list[Field], source_file: str | None):
    lines.append(f"### `{table}`")
    lines.append("")
    lines.append(f"Role: {TABLE_PURPOSES.get(table, 'Table metier de l\'application.')}" )
    if source_file:
        lines.append(f"Source: `{source_file}`")
    lines.append("")
    lines.append("| Champ | Type C# | Nullable | Role | Description |")
    lines.append("|---|---|---|---|---|")
    for field in fields:
        lines.append(f"| `{field.name}` | `{field.type_name}` | {format_nullable(field)} | {field.role} | {describe_field(field)} |")
    notes = TABLE_NOTES.get(table)
    if notes:
        lines.append("")
        lines.append("Contraintes / notes:")
        for note in notes:
            lines.append(f"- {note}")
    lines.append("")


def build_dictionary() -> str:
    dbsets = parse_dbsets()
    class_fields, enum_names, class_files = parse_entities()

    table_to_class = {table: cls for cls, table in dbsets.items()}

    lines: list[str] = []
    lines.append("# Dictionnaire de donnees - MangoTaika")
    lines.append("")
    lines.append(f"Date de generation: {date.today().isoformat()}")
    lines.append("")
    lines.append("## Sources")
    lines.append("")
    lines.append("- `Data/AppDbContext.cs`")
    lines.append("- `Data/Entities/*.cs`")
    lines.append("- conventions EF Core et ASP.NET Identity du projet")
    lines.append("")
    lines.append("## Legende")
    lines.append("")
    lines.append("- `PK` : cle primaire")
    lines.append("- `FK` : cle etrangere")
    lines.append("- `Enum` : champ base sur une enumeration C#")
    lines.append("- `Flag` : booleen de statut")
    lines.append("- `Nullable` : accepte ou non les valeurs nulles dans le modele")
    lines.append("")

    for domain in DOMAIN_ORDER:
        lines.append(f"## {domain}")
        lines.append("")
        for table in DOMAIN_TABLES[domain]:
            if table == "ParentScout":
                write_section(lines, table, [
                    Field("ParentsId", "Guid", False, "PK/FK"),
                    Field("ScoutsId", "Guid", False, "PK/FK"),
                ], "Association implicite EF Core Scout <-> Parent")
                continue

            if table.startswith("AspNet") and table != "AspNetUsers":
                lines.append(f"### `{table}`")
                lines.append("")
                lines.append(f"Role: {TABLE_PURPOSES.get(table, 'Table technique ASP.NET Identity.')}")
                lines.append("")
                lines.append("Table technique geree par ASP.NET Identity. Sa structure exacte depend des conventions Identity / EF Core du projet.")
                lines.append("")
                continue

            class_name = table_to_class.get(table)
            source_file = class_files.get(class_name, None) if class_name else None
            fields = class_fields.get(class_name, []) if class_name else []

            if table == "AspNetUsers":
                identity_base_fields = [
                    Field("Id", "Guid", False, "PK"),
                    Field("UserName", "string?", True, "Metier"),
                    Field("NormalizedUserName", "string?", True, "Metier"),
                    Field("Email", "string?", True, "Metier"),
                    Field("NormalizedEmail", "string?", True, "Metier"),
                    Field("EmailConfirmed", "bool", False, "Flag"),
                    Field("PasswordHash", "string?", True, "Metier"),
                    Field("PhoneNumber", "string?", True, "Metier"),
                    Field("PhoneNumberConfirmed", "bool", False, "Flag"),
                    Field("TwoFactorEnabled", "bool", False, "Flag"),
                    Field("LockoutEnd", "DateTime?", True, "Date"),
                    Field("LockoutEnabled", "bool", False, "Flag"),
                    Field("AccessFailedCount", "int", False, "Metier"),
                ]
                write_section(lines, table, identity_base_fields + fields, source_file)
                continue

            write_section(lines, table, fields, source_file)

    lines.append("## Enumerations metier")
    lines.append("")
    enum_defs = {
        "StatutWorkflowDocument": ["Brouillon", "Soumis", "AReviser", "Valide"],
        "StatutInscriptionAnnuelle": ["Enregistree", "Validee", "Suspendue"],
        "StatutLigneCotisationNationale": ["Ajour", "NonAjour", "AVerifier"],
    }
    for enum_name, values in enum_defs.items():
        lines.append(f"- `{enum_name}` : {', '.join(f'`{value}`' for value in values)}")

    lines.append("")
    lines.append("## Remarques")
    lines.append("")
    lines.append("- Ce dictionnaire est centre sur le modele de donnees du depot et ses conventions EF Core.")
    lines.append("- Les tables physiques peuvent contenir des colonnes supplementaires generees par EF Core ou PostgreSQL selon les migrations.")
    lines.append("- `CompetenceLieeId` sur `Formations` est documente comme reference logique car aucune FK explicite n'est configuree dans `OnModelCreating`.")
    return "\n".join(lines) + "\n"


def main() -> None:
    OUTPUT.write_text(build_dictionary(), encoding="utf-8")
    print(f"Generated {OUTPUT}")


if __name__ == "__main__":
    main()


