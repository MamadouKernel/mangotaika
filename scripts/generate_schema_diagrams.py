from __future__ import annotations

import html
import uuid
from dataclasses import dataclass, field
from pathlib import Path
from xml.etree import ElementTree as ET

ROOT = Path(r"C:\Users\kerne\Downloads\rodi\new\MangoTaika")
DOCS = ROOT / "docs"

HEADER_FILL = "#5f7f43"
BOX_FILL = "#f8fbf4"
BOX_STROKE = "#5f7f43"
TEXT_COLOR = "#27313f"
MUTED_TEXT = "#5b6775"
EDGE_COLOR = "#7f8b99"
CANVAS_FILL = "#f5f7fb"
DOMAIN_FILL = "#eef5e4"
DOMAIN_STROKE = "#cddbb8"


@dataclass
class Node:
    id: str
    title: str
    x: int
    y: int
    w: int
    h: int
    fields: list[str] = field(default_factory=list)
    kind: str = "entity"


@dataclass
class Edge:
    src: str
    dst: str
    label: str
    src_mult: str = ""
    dst_mult: str = ""


@dataclass
class Diagram:
    key: str
    page_name: str
    title: str
    width: int
    height: int
    nodes: list[Node]
    edges: list[Edge]


def esc(text: str) -> str:
    return html.escape(text, quote=True)


def drawio_label(node: Node) -> str:
    if node.kind == "domain":
        return f"<div style='font-size:17px;font-weight:700;color:{BOX_STROKE}'>{esc(node.title)}</div>"
    lines = "<br/>".join(esc(line) for line in node.fields)
    return (
        f"<div style='font-size:15px;font-weight:700;color:{TEXT_COLOR};margin-bottom:6px'>{esc(node.title)}</div>"
        f"<div style='font-size:10px;color:{MUTED_TEXT};line-height:1.35'>{lines}</div>"
    )


def build_drawio(diagrams: list[Diagram]) -> ET.ElementTree:
    mxfile = ET.Element(
        "mxfile",
        {
            "host": "app.diagrams.net",
            "modified": "2026-04-01T08:00:00.000Z",
            "agent": "Codex",
            "version": "24.7.17",
            "compressed": "false",
        },
    )

    for diagram in diagrams:
        diag_el = ET.SubElement(mxfile, "diagram", {"id": str(uuid.uuid4()), "name": diagram.page_name})
        model = ET.SubElement(
            diag_el,
            "mxGraphModel",
            {
                "dx": str(diagram.width),
                "dy": str(diagram.height),
                "grid": "1",
                "gridSize": "10",
                "guides": "1",
                "tooltips": "1",
                "connect": "1",
                "arrows": "1",
                "fold": "1",
                "page": "1",
                "pageScale": "1",
                "pageWidth": str(max(1100, diagram.width)),
                "pageHeight": str(max(850, diagram.height)),
                "math": "0",
                "shadow": "0",
            },
        )
        root = ET.SubElement(model, "root")
        ET.SubElement(root, "mxCell", {"id": "0"})
        ET.SubElement(root, "mxCell", {"id": "1", "parent": "0"})

        title_cell = ET.SubElement(
            root,
            "mxCell",
            {
                "id": f"title-{diagram.key}",
                "value": esc(diagram.title),
                "style": "text;html=1;strokeColor=none;fillColor=none;align=left;verticalAlign=top;fontSize=22;fontStyle=1;fontColor=#27313f;",
                "vertex": "1",
                "parent": "1",
            },
        )
        title_cell.append(ET.Element("mxGeometry", {"x": "40", "y": "20", "width": str(diagram.width - 80), "height": "30", "as": "geometry"}))

        for node in diagram.nodes:
            if node.kind == "domain":
                style = (
                    f"rounded=1;whiteSpace=wrap;html=1;fillColor={DOMAIN_FILL};strokeColor={DOMAIN_STROKE};"
                    "fontSize=18;fontStyle=1;spacing=12;"
                )
            else:
                style = (
                    f"rounded=1;whiteSpace=wrap;html=1;fillColor={BOX_FILL};strokeColor={BOX_STROKE};"
                    "fontSize=12;align=left;verticalAlign=top;spacing=14;arcSize=10;"
                )
            cell = ET.SubElement(
                root,
                "mxCell",
                {
                    "id": node.id,
                    "value": drawio_label(node),
                    "style": style,
                    "vertex": "1",
                    "parent": "1",
                },
            )
            cell.append(ET.Element("mxGeometry", {"x": str(node.x), "y": str(node.y), "width": str(node.w), "height": str(node.h), "as": "geometry"}))

        for idx, edge in enumerate(diagram.edges, start=1):
            ecell = ET.SubElement(
                root,
                "mxCell",
                {
                    "id": f"edge-{diagram.key}-{idx}",
                    "value": esc(edge.label),
                    "style": f"edgeStyle=orthogonalEdgeStyle;rounded=1;orthogonalLoop=1;jettySize=auto;html=1;strokeColor={EDGE_COLOR};fontSize=10;labelBackgroundColor=#ffffff;endArrow=none;startArrow=none;",
                    "edge": "1",
                    "parent": "1",
                    "source": edge.src,
                    "target": edge.dst,
                },
            )
            ecell.append(ET.Element("mxGeometry", {"relative": "1", "as": "geometry"}))

    return ET.ElementTree(mxfile)


def anchor(n1: Node, n2: Node) -> tuple[tuple[float, float], tuple[float, float]]:
    c1x = n1.x + n1.w / 2
    c1y = n1.y + n1.h / 2
    c2x = n2.x + n2.w / 2
    c2y = n2.y + n2.h / 2
    dx = c2x - c1x
    dy = c2y - c1y
    if abs(dx) > abs(dy):
        p1 = (n1.x + n1.w, c1y) if dx >= 0 else (n1.x, c1y)
        p2 = (n2.x, c2y) if dx >= 0 else (n2.x + n2.w, c2y)
    else:
        p1 = (c1x, n1.y + n1.h) if dy >= 0 else (c1x, n1.y)
        p2 = (c2x, n2.y) if dy >= 0 else (c2x, n2.y + n2.h)
    return p1, p2


def svg_text_lines(svg_parts: list[str], x: int, y: int, lines: list[str], size: int, color: str, weight: str = "400", line_height: int = 16):
    svg_parts.append(f"<text x='{x}' y='{y}' font-family='Segoe UI, Arial, sans-serif' font-size='{size}' font-weight='{weight}' fill='{color}'>")
    for i, line in enumerate(lines):
        dy = 0 if i == 0 else line_height
        svg_parts.append(f"<tspan x='{x}' dy='{dy}'>{esc(line)}</tspan>")
    svg_parts.append("</text>")


def build_svg(diagram: Diagram) -> str:
    parts: list[str] = []
    parts.append(f"<svg xmlns='http://www.w3.org/2000/svg' width='{diagram.width}' height='{diagram.height}' viewBox='0 0 {diagram.width} {diagram.height}'>")
    parts.append("<defs>")
    parts.append("<filter id='shadow' x='-20%' y='-20%' width='140%' height='140%'><feDropShadow dx='0' dy='10' stdDeviation='10' flood-color='#b7c2cf' flood-opacity='0.22'/></filter>")
    parts.append("</defs>")
    parts.append(f"<rect x='0' y='0' width='{diagram.width}' height='{diagram.height}' fill='{CANVAS_FILL}'/>")
    svg_text_lines(parts, 40, 44, [diagram.title], 24, TEXT_COLOR, "700", 28)

    lookup = {node.id: node for node in diagram.nodes}
    for edge in diagram.edges:
        n1 = lookup[edge.src]
        n2 = lookup[edge.dst]
        (x1, y1), (x2, y2) = anchor(n1, n2)
        mx = (x1 + x2) / 2
        my = (y1 + y2) / 2
        parts.append(f"<line x1='{x1:.1f}' y1='{y1:.1f}' x2='{x2:.1f}' y2='{y2:.1f}' stroke='{EDGE_COLOR}' stroke-width='2.2' stroke-linecap='round'/>")
        if edge.src_mult:
            parts.append(f"<text x='{x1 + 6:.1f}' y='{y1 - 6:.1f}' font-family='Segoe UI, Arial, sans-serif' font-size='10' font-weight='700' fill='{BOX_STROKE}'>{esc(edge.src_mult)}</text>")
        if edge.dst_mult:
            parts.append(f"<text x='{x2 + 6:.1f}' y='{y2 - 6:.1f}' font-family='Segoe UI, Arial, sans-serif' font-size='10' font-weight='700' fill='{BOX_STROKE}'>{esc(edge.dst_mult)}</text>")
        parts.append(f"<rect x='{mx - 58:.1f}' y='{my - 11:.1f}' rx='10' ry='10' width='116' height='22' fill='#ffffff' opacity='0.92'/>")
        parts.append(f"<text x='{mx:.1f}' y='{my + 4:.1f}' text-anchor='middle' font-family='Segoe UI, Arial, sans-serif' font-size='10.5' font-weight='600' fill='{MUTED_TEXT}'>{esc(edge.label)}</text>")

    for node in diagram.nodes:
        if node.kind == "domain":
            parts.append(f"<rect x='{node.x}' y='{node.y}' width='{node.w}' height='{node.h}' rx='26' ry='26' fill='{DOMAIN_FILL}' stroke='{DOMAIN_STROKE}' stroke-width='2'/>")
            svg_text_lines(parts, node.x + 20, node.y + 36, [node.title], 18, BOX_STROKE, "700", 22)
            continue

        parts.append(f"<rect x='{node.x}' y='{node.y}' width='{node.w}' height='{node.h}' rx='18' ry='18' fill='{BOX_FILL}' stroke='{BOX_STROKE}' stroke-width='2' filter='url(#shadow)'/>")
        parts.append(f"<rect x='{node.x}' y='{node.y}' width='{node.w}' height='38' rx='18' ry='18' fill='{HEADER_FILL}'/>")
        parts.append(f"<rect x='{node.x}' y='{node.y + 20}' width='{node.w}' height='18' fill='{HEADER_FILL}'/>")
        svg_text_lines(parts, node.x + 14, node.y + 24, [node.title], 16, '#ffffff', '700', 18)
        svg_text_lines(parts, node.x + 14, node.y + 58, node.fields, 11, TEXT_COLOR, '400', 15)

    parts.append("</svg>")
    return "".join(parts)


def write_svg(diagram: Diagram):
    output = DOCS / f"{diagram.key}.svg"
    output.write_text(build_svg(diagram), encoding="utf-8")


def make_diagrams() -> list[Diagram]:
    mcd_nodes = [
        Node("territoire", "Territoire Scout", 60, 110, 260, 72, kind="domain"),
        Node("groupes", "Groupes", 100, 230, 220, 120, ["entites scouts", "adresse", "chef / district", "membres"]),
        Node("branches", "Branches", 390, 230, 220, 120, ["unites d'age", "chef d'unite", "groupe parent"]),
        Node("scouts", "Scouts", 685, 205, 250, 145, ["fiches membres", "fonction", "groupe", "branche", "carte / cotisation"]),
        Node("parents", "Parents", 1010, 230, 220, 110, ["contacts familiaux", "relation parentale"]),
        Node("parcours", "Parcours et conformite", 520, 415, 300, 82, kind="domain"),
        Node("etapes", "Etapes parcours scout", 430, 555, 240, 120, ["jalons du parcours", "prevision", "validation"]),
        Node("inscriptions", "Inscriptions annuelles", 740, 555, 250, 125, ["annee de reference", "validation paroissiale", "cotisation nationale"]),
        Node("ops", "Activites et gouvernance", 60, 415, 280, 82, kind="domain"),
        Node("activites", "Activites", 90, 555, 220, 120, ["sorties", "participants", "documents", "rapport d'activite"]),
        Node("programmes", "Programmes / Maitrise", 1010, 555, 250, 125, ["programme annuel", "proposition de maitrise", "validation"]),
        Node("support", "Support et finance", 60, 715, 280, 82, kind="domain"),
        Node("tickets", "Tickets support", 120, 850, 220, 120, ["centre de support", "messages", "historique"]),
        Node("finance", "Cotisations et finances", 430, 850, 260, 125, ["transactions", "imports cotisations", "AGR"]),
        Node("lms", "LMS et formation", 760, 715, 250, 82, kind="domain"),
        Node("formations", "Formations", 800, 850, 240, 130, ["modules", "lecons", "quiz", "certifications"]),
        Node("communication", "Communication", 1090, 715, 220, 82, kind="domain"),
        Node("contenus", "Contenus publics", 1080, 850, 240, 120, ["actualites", "galerie", "mot du commissaire", "partenaires"]),
    ]
    mcd_edges = [
        Edge("groupes", "branches", "contient", "1", "N"),
        Edge("groupes", "scouts", "rattache", "1", "N"),
        Edge("branches", "scouts", "organise", "1", "N"),
        Edge("scouts", "parents", "est lie a", "N", "N"),
        Edge("scouts", "etapes", "progresse dans", "1", "N"),
        Edge("scouts", "inscriptions", "renouvelle", "1", "N"),
        Edge("groupes", "activites", "porte", "1", "N"),
        Edge("activites", "programmes", "alimente", "N", "1"),
        Edge("groupes", "programmes", "planifie / propose", "1", "N"),
        Edge("groupes", "tickets", "concerne", "1", "N"),
        Edge("scouts", "finance", "cotise", "1", "N"),
        Edge("scouts", "formations", "suit", "1", "N"),
        Edge("branches", "formations", "cible", "1", "N"),
        Edge("territoire", "groupes", "structure"),
        Edge("support", "tickets", "inclut"),
        Edge("support", "finance", "inclut"),
        Edge("lms", "formations", "inclut"),
        Edge("communication", "contenus", "inclut"),
    ]

    core_nodes = [
        Node("u", "AspNetUsers", 60, 140, 250, 220, ["PK Id", "UserName", "Email", "PhoneNumber", "Nom", "Prenom", "Matricule", "IsActive", "FK GroupeId", "FK BrancheId"]),
        Node("g", "Groupes", 375, 120, 255, 245, ["PK Id", "Nom", "UK NomNormalise", "Description", "Adresse", "NomChefGroupe", "LogoUrl", "Latitude", "Longitude", "IsActive", "FK ResponsableId"]),
        Node("b", "Branches", 700, 120, 255, 225, ["PK Id", "Nom", "UK NomNormalise", "Description", "LogoUrl", "AgeMin", "AgeMax", "NomChefUnite", "IsActive", "FK GroupeId", "FK ChefUniteId"]),
        Node("s", "Scouts", 1020, 90, 285, 320, ["PK Id", "UK Matricule", "Nom", "Prenom", "DateNaissance", "Sexe", "Telephone", "Email", "PhotoUrl", "UK NumeroCarte", "Fonction", "StatutASCCI", "AssuranceAnnuelle", "IsActive", "FK UserId", "FK GroupeId", "FK BrancheId"]),
        Node("p", "Parents", 1370, 120, 220, 150, ["PK Id", "Nom", "Prenom", "Telephone", "Email", "Relation"]),
        Node("ps", "ParentScout", 1370, 320, 220, 120, ["PK/FK ParentsId", "PK/FK ScoutsId"]),
        Node("c", "Competences", 1020, 460, 240, 150, ["PK Id", "Nom", "Description", "DateObtention", "Niveau", "FK ScoutId"]),
        Node("hf", "HistoriqueFonctions", 700, 390, 255, 195, ["PK Id", "Fonction", "DateDebut", "DateFin", "Commentaire", "FK ScoutId", "FK UserId", "FK GroupeId"]),
        Node("sa", "SuivisAcademiques", 1020, 655, 270, 185, ["PK Id", "AnneeScolaire", "Etablissement", "NiveauScolaire", "Classe", "MoyenneGenerale", "Mention", "EstRedoublant", "FK ScoutId"]),
        Node("ci", "CodesInvitation", 375, 430, 255, 175, ["PK Id", "UK Code", "EstUtilise", "DateCreation", "DateUtilisation", "FK CreateurId", "FK UtilisePaId"]),
    ]
    core_edges = [
        Edge("g", "b", "contient", "1", "N"),
        Edge("g", "s", "rattache", "1", "N"),
        Edge("b", "s", "organise", "1", "N"),
        Edge("u", "g", "responsable / membre", "N", "1"),
        Edge("u", "b", "affectation", "N", "1"),
        Edge("s", "b", "chef unite", "N", "1"),
        Edge("s", "c", "possede", "1", "N"),
        Edge("s", "hf", "historise", "1", "N"),
        Edge("g", "hf", "contexte", "1", "N"),
        Edge("s", "sa", "suit", "1", "N"),
        Edge("s", "ps", "jointure", "1", "N"),
        Edge("p", "ps", "jointure", "1", "N"),
        Edge("u", "ci", "cree / utilise", "1", "N"),
        Edge("s", "u", "compte associe", "N", "1"),
    ]

    annual_nodes = [
        Node("sc", "Scouts", 60, 130, 240, 170, ["PK Id", "Matricule", "Nom", "Prenom", "Fonction", "FK GroupeId", "FK BrancheId"]),
        Node("gr", "Groupes", 360, 115, 220, 140, ["PK Id", "Nom", "NomChefGroupe", "ResponsableId"]),
        Node("brc", "Branches", 640, 115, 220, 145, ["PK Id", "Nom", "AgeMin", "AgeMax", "GroupeId", "ChefUniteId"]),
        Node("eta", "EtapesParcoursScouts", 60, 395, 260, 190, ["PK Id", "FK ScoutId", "NomEtape", "CodeEtape", "OrdreAffichage", "DateValidation", "DatePrevisionnelle", "Observations", "EstObligatoire"]),
        Node("insa", "InscriptionsAnnuellesScouts", 365, 360, 285, 245, ["PK Id", "FK ScoutId", "FK GroupeId", "FK BrancheId", "FonctionSnapshot", "AnneeReference", "LibelleAnnee", "DateInscription", "DateValidation", "Statut", "InscriptionParoissialeValidee", "CotisationNationaleAjour", "Observations", "FK ValideParId"]),
        Node("prog", "ProgrammesAnnuels", 710, 360, 270, 225, ["PK Id", "FK GroupeId", "AnneeReference", "Titre", "Objectifs", "CalendrierSynthese", "Statut", "CommentaireValidation", "DateCreation", "DateSoumission", "DateValidation", "FK CreateurId", "FK ValideurId"]),
        Node("act", "Activites", 1040, 130, 240, 165, ["PK Id", "Titre", "DateDebut", "DateFin", "Lieu", "BudgetPrevisionnel", "FK GroupeId", "FK CreateurId"]),
        Node("rap", "RapportsActivite", 1040, 390, 280, 225, ["PK Id", "FK ActiviteId", "ResumeExecutif", "ResultatsObtenus", "DifficultesRencontrees", "Recommandations", "Statut", "CommentaireValidation", "DateCreation", "DateSoumission", "DateValidation", "FK CreateurId", "FK ValideurId"]),
        Node("mait", "PropositionsMaitriseAnnuelles", 1360, 360, 295, 225, ["PK Id", "FK GroupeId", "AnneeReference", "Titre", "CompositionProposee", "ObjectifsPedagogiques", "BesoinsFormation", "Statut", "DateCreation", "DateSoumission", "DateValidation", "FK CreateurId", "FK ValideurId"]),
        Node("cotimp", "CotisationsNationalesImports", 365, 690, 280, 205, ["PK Id", "AnneeReference", "NomFichier", "DateImport", "MontantTotal", "NombreAjour", "NombreNonAjour", "NombreAVerifier", "FK CreateurId"]),
        Node("cotlig", "CotisationsNationalesImportLignes", 710, 700, 285, 185, ["PK Id", "FK ImportId", "FK ScoutId", "Matricule", "NomImporte", "Montant", "Statut", "Motif"]),
        Node("tf", "TransactionsFinancieres", 1040, 700, 270, 185, ["PK Id", "Libelle", "Montant", "DateTransaction", "Reference", "FK GroupeId", "FK ActiviteId", "FK ProjetAGRId", "FK ScoutId", "FK CreateurId"]),
    ]
    annual_edges = [
        Edge("sc", "eta", "progresse", "1", "N"),
        Edge("sc", "insa", "renouvelle", "1", "N"),
        Edge("gr", "insa", "snapshot groupe", "1", "N"),
        Edge("brc", "insa", "snapshot branche", "1", "N"),
        Edge("gr", "prog", "planifie", "1", "N"),
        Edge("act", "rap", "cloture", "1", "1"),
        Edge("gr", "mait", "propose", "1", "N"),
        Edge("cotimp", "cotlig", "contient", "1", "N"),
        Edge("sc", "cotlig", "rapproche", "1", "N"),
        Edge("sc", "tf", "cotise", "1", "N"),
        Edge("act", "tf", "alimente", "1", "N"),
    ]

    ops_nodes = [
        Node("ga", "Groupes", 60, 120, 220, 155, ["PK Id", "Nom", "Adresse", "ResponsableId", "IsActive"]),
        Node("ua", "AspNetUsers", 330, 90, 235, 175, ["PK Id", "Nom", "Prenom", "Email", "FK GroupeId", "FK BrancheId"]),
        Node("sc2", "Scouts", 615, 90, 230, 170, ["PK Id", "Matricule", "Nom", "Prenom", "FK GroupeId", "FK BrancheId"]),
        Node("act2", "Activites", 60, 360, 245, 205, ["PK Id", "Titre", "Description", "DateDebut", "DateFin", "Lieu", "BudgetPrevisionnel", "NomResponsable", "FK CreateurId", "FK GroupeId"]),
        Node("doc", "DocumentsActivite", 360, 385, 230, 155, ["PK Id", "NomFichier", "CheminFichier", "TypeDocument", "DateUpload", "FK ActiviteId"]),
        Node("part", "ParticipantsActivite", 640, 375, 230, 150, ["PK Id", "FK ActiviteId", "FK ScoutId", "DateInscription"]),
        Node("comm", "CommentairesActivite", 910, 360, 250, 165, ["PK Id", "FK ActiviteId", "FK AuteurId", "Contenu", "TypeAction", "DateCreation"]),
        Node("dem", "DemandesAutorisation", 60, 650, 270, 235, ["PK Id", "Titre", "DateActivite", "DateFin", "Lieu", "NombreParticipants", "Budget", "TdrContenu", "MotifRejet", "DateCreation", "FK DemandeurId", "FK ValideurId", "FK GroupeId"]),
        Node("sd", "SuivisDemande", 380, 700, 220, 130, ["PK Id", "FK DemandeId", "Commentaire", "Auteur", "Date"]),
        Node("dg", "DemandesGroupe", 650, 665, 240, 170, ["PK Id", "NomGroupe", "Commune", "Quartier", "NomResponsable", "TelephoneResponsable", "NombreMembresPrevus", "FK TraiteParId"]),
        Node("svc", "SupportCatalogueServices", 1220, 90, 270, 195, ["PK Id", "UK Code", "Nom", "Description", "DelaiSlaHeures", "FK AssigneParDefautId", "FK GroupeParDefautId", "FK AuteurId", "EstActif"]),
        Node("kb", "SupportKnowledgeArticles", 1535, 95, 225, 170, ["PK Id", "Titre", "Resume", "Contenu", "Categorie", "MotsCles", "EstPublie", "FK AuteurId"]),
        Node("tic", "Tickets", 1220, 360, 275, 245, ["PK Id", "NumeroTicket", "Sujet", "Description", "DateCreation", "DateLimiteSla", "DatePremiereReponse", "DateAffectation", "DateResolution", "EstEscalade", "NiveauEscalade", "NoteSatisfaction", "FK ServiceCatalogueId", "FK CreateurId", "FK AssigneAId", "FK GroupeAssigneId"]),
        Node("msg", "MessagesTicket", 1535, 350, 225, 145, ["PK Id", "FK TicketId", "FK AuteurId", "Contenu", "EstNoteInterne", "DateEnvoi"]),
        Node("pj", "TicketPiecesJointes", 1535, 520, 235, 160, ["PK Id", "FK TicketId", "FK AjouteParId", "NomOriginal", "UrlFichier", "TypeMime", "TailleOctets"]),
        Node("ht", "HistoriquesTicket", 1220, 650, 245, 145, ["PK Id", "FK TicketId", "FK AuteurId", "Commentaire", "DateChangement"]),
        Node("notif", "NotificationsUtilisateur", 1535, 710, 230, 150, ["PK Id", "FK UserId", "Titre", "Message", "Categorie", "Lien", "EstLue"]),
        Node("agr", "ProjetsAGR", 910, 650, 250, 180, ["PK Id", "Nom", "Description", "BudgetInitial", "DateDebut", "DateFin", "Responsable", "FK GroupeId", "FK CreateurId"]),
        Node("tf2", "TransactionsFinancieres", 910, 880, 285, 195, ["PK Id", "Libelle", "Montant", "DateTransaction", "Reference", "Commentaire", "FK GroupeId", "FK ActiviteId", "FK ProjetAGRId", "FK ScoutId", "FK CreateurId"]),
    ]
    ops_edges = [
        Edge("ga", "act2", "porte", "1", "N"),
        Edge("ua", "act2", "cree", "1", "N"),
        Edge("act2", "doc", "contient", "1", "N"),
        Edge("act2", "part", "recoit", "1", "N"),
        Edge("sc2", "part", "participe", "1", "N"),
        Edge("act2", "comm", "journalise", "1", "N"),
        Edge("ua", "comm", "auteur", "1", "N"),
        Edge("ga", "dem", "concerne", "1", "N"),
        Edge("ua", "dem", "demande / valide", "1", "N"),
        Edge("dem", "sd", "trace", "1", "N"),
        Edge("ua", "dg", "traite", "1", "N"),
        Edge("svc", "tic", "qualifie", "1", "N"),
        Edge("svc", "kb", "alimente", "1", "N"),
        Edge("ua", "tic", "cree / assigne", "1", "N"),
        Edge("ga", "tic", "groupe assigne", "1", "N"),
        Edge("tic", "msg", "contient", "1", "N"),
        Edge("tic", "pj", "attache", "1", "N"),
        Edge("tic", "ht", "historise", "1", "N"),
        Edge("ua", "notif", "recoit", "1", "N"),
        Edge("ga", "agr", "porte", "1", "N"),
        Edge("agr", "tf2", "genere", "1", "N"),
        Edge("act2", "tf2", "finance", "1", "N"),
        Edge("sc2", "tf2", "concerne", "1", "N"),
        Edge("ga", "tf2", "rattache", "1", "N"),
    ]

    lms_nodes = [
        Node("ub", "AspNetUsers", 60, 90, 235, 160, ["PK Id", "Nom", "Prenom", "Email", "FK GroupeId", "FK BrancheId"]),
        Node("br", "Branches", 340, 90, 235, 150, ["PK Id", "Nom", "AgeMin", "AgeMax", "FK GroupeId", "FK ChefUniteId"]),
        Node("sf", "Scouts", 620, 90, 245, 165, ["PK Id", "Matricule", "Nom", "Prenom", "Fonction", "FK GroupeId", "FK BrancheId"]),
        Node("fo", "Formations", 340, 310, 280, 250, ["PK Id", "Titre", "Description", "ImageUrl", "Niveau", "Statut", "DureeEstimeeHeures", "DateCreation", "DatePublication", "DelivreBadge", "DelivreAttestation", "DelivreCertificat", "FK BrancheCibleId", "CompetenceLieeId", "FK AuteurId"]),
        Node("pr", "FormationsPrerequis", 60, 380, 230, 125, ["PK/FK FormationId", "PK/FK PrerequisFormationId"]),
        Node("mo", "ModulesFormation", 670, 320, 240, 145, ["PK Id", "Titre", "Description", "Ordre", "FK FormationId"]),
        Node("le", "Lecons", 955, 310, 235, 160, ["PK Id", "Titre", "ContenuTexte", "VideoUrl", "DocumentUrl", "Ordre", "DureeMinutes", "FK ModuleId"]),
        Node("qu", "Quizzes", 955, 510, 235, 160, ["PK Id", "Titre", "NoteMinimale", "NombreTentativesMax", "DateOuvertureDisponibilite", "DateFermetureDisponibilite", "FK ModuleId"]),
        Node("qq", "QuestionsQuiz", 1235, 500, 220, 130, ["PK Id", "Enonce", "Ordre", "FK QuizId"]),
        Node("rq", "ReponsesQuiz", 1490, 500, 220, 130, ["PK Id", "Texte", "EstCorrecte", "Ordre", "FK QuestionId"]),
        Node("ses", "SessionsFormation", 60, 640, 240, 155, ["PK Id", "Titre", "Description", "EstSelfPaced", "EstPubliee", "DateOuverture", "DateFermeture", "FK FormationId"]),
        Node("ins", "InscriptionsFormation", 340, 650, 255, 170, ["PK Id", "DateInscription", "DateTerminee", "ProgressionPourcent", "FK ScoutId", "FK FormationId", "FK SessionFormationId"]),
        Node("prog2", "ProgressionsLecon", 635, 700, 235, 140, ["PK Id", "EstTerminee", "DateTerminee", "FK ScoutId", "FK LeconId"]),
        Node("tent", "TentativesQuiz", 915, 710, 235, 145, ["PK Id", "Score", "Reussi", "DateTentative", "ReponsesJson", "FK ScoutId", "FK QuizId"]),
        Node("cert", "CertificationsFormation", 1200, 700, 265, 165, ["PK Id", "UK Code", "DateEmission", "ScoreFinal", "Mention", "FK ScoutId", "FK FormationId", "FK InscriptionFormationId"]),
        Node("jal", "JalonsFormation", 60, 870, 230, 135, ["PK Id", "FK FormationId", "Titre", "Description", "DateJalon", "EstPublie"]),
        Node("ann", "AnnoncesFormation", 340, 880, 245, 135, ["PK Id", "Titre", "Contenu", "EstPubliee", "DatePublication", "FK FormationId", "FK AuteurId"]),
        Node("dis", "DiscussionsFormation", 635, 900, 250, 150, ["PK Id", "Titre", "ContenuInitial", "DateCreation", "DateDerniereActivite", "EstVerrouillee", "FK FormationId", "FK AuteurId"]),
        Node("mdf", "MessagesDiscussionFormation", 930, 915, 280, 140, ["PK Id", "Contenu", "DateCreation", "EstSupprime", "FK DiscussionFormationId", "FK AuteurId"]),
    ]
    lms_edges = [
        Edge("br", "fo", "branche cible", "1", "N"),
        Edge("ub", "fo", "auteur", "1", "N"),
        Edge("fo", "pr", "prerequis", "1", "N"),
        Edge("fo", "mo", "contient", "1", "N"),
        Edge("mo", "le", "contient", "1", "N"),
        Edge("mo", "qu", "porte", "1", "1"),
        Edge("qu", "qq", "contient", "1", "N"),
        Edge("qq", "rq", "propose", "1", "N"),
        Edge("fo", "ses", "publie", "1", "N"),
        Edge("sf", "ins", "suit", "1", "N"),
        Edge("fo", "ins", "ouvre", "1", "N"),
        Edge("ses", "ins", "cadre", "1", "N"),
        Edge("sf", "prog2", "valide", "1", "N"),
        Edge("le", "prog2", "est suivie", "1", "N"),
        Edge("sf", "tent", "tente", "1", "N"),
        Edge("qu", "tent", "est tente", "1", "N"),
        Edge("sf", "cert", "recoit", "1", "N"),
        Edge("fo", "cert", "delivre", "1", "N"),
        Edge("ins", "cert", "justifie", "1", "N"),
        Edge("fo", "jal", "planifie", "1", "N"),
        Edge("fo", "ann", "annonce", "1", "N"),
        Edge("ub", "ann", "auteur", "1", "N"),
        Edge("fo", "dis", "anime", "1", "N"),
        Edge("ub", "dis", "ouvre", "1", "N"),
        Edge("dis", "mdf", "contient", "1", "N"),
        Edge("ub", "mdf", "redige", "1", "N"),
    ]

    communication_nodes = [
        Node("uc", "AspNetUsers", 70, 145, 240, 170, ["PK Id", "Nom", "Prenom", "Email", "PhotoUrl", "IsActive"]),
        Node("actu", "Actualites", 390, 120, 255, 185, ["PK Id", "Titre", "Contenu", "ImageUrl", "Resume", "DatePublication", "EstPublie", "EstSupprime", "DateCreation", "FK CreateurId"]),
        Node("gal", "Galeries", 715, 120, 240, 170, ["PK Id", "Titre", "Description", "CheminMedia", "TypeMedia", "DateUpload", "EstPublie", "EstSupprime"]),
        Node("mot", "MotsCommissaire", 1015, 120, 240, 165, ["PK Id", "Titre", "Contenu", "PhotoUrl", "Annee", "EstActif", "DateCreation"]),
        Node("livre", "LivreDor", 1325, 120, 240, 165, ["PK Id", "NomAuteur", "Message", "DateCreation", "EstValide", "DateValidation"]),
        Node("contact", "ContactMessages", 230, 430, 250, 170, ["PK Id", "Nom", "Email", "Sujet", "Message", "Type", "DateEnvoi", "EstTraite"]),
        Node("hist", "MembresHistoriques", 555, 430, 255, 165, ["PK Id", "Nom", "PhotoUrl", "Description", "Periode", "Ordre"]),
        Node("part", "Partenaires", 885, 430, 250, 180, ["PK Id", "Nom", "Description", "LogoUrl", "SiteWeb", "TypePartenariat", "EstActif", "EstSupprime", "Ordre", "DateCreation"]),
        Node("rs", "LiensReseauxSociaux", 1210, 430, 250, 155, ["PK Id", "Plateforme", "Url", "Icone", "EstActif", "Ordre"]),
        Node("public", "Portail public", 620, 705, 260, 82, kind="domain"),
    ]
    communication_edges = [
        Edge("uc", "actu", "publie", "1", "N"),
        Edge("public", "actu", "expose"),
        Edge("public", "gal", "expose"),
        Edge("public", "mot", "expose"),
        Edge("public", "livre", "collecte"),
        Edge("public", "contact", "collecte"),
        Edge("public", "hist", "valorise"),
        Edge("public", "part", "valorise"),
        Edge("public", "rs", "diffuse"),
    ]

    return [
        Diagram("schema-mcd-global", "MCD Global", "MangoTaika - MCD global complet", 1400, 1040, mcd_nodes, mcd_edges),
        Diagram("schema-mld-coeur-territoire", "MLD Coeur", "MangoTaika - MLD coeur scout et territoire", 1640, 920, core_nodes, core_edges),
        Diagram("schema-mld-parcours-conformite-annuelle", "MLD Parcours", "MangoTaika - MLD parcours scout et conformite annuelle", 1700, 980, annual_nodes, annual_edges),
        Diagram("schema-mld-operations-support-finance", "MLD Operations", "MangoTaika - MLD activites, demandes, support et finance", 1820, 1120, ops_nodes, ops_edges),
        Diagram("schema-mld-lms-formation", "MLD LMS", "MangoTaika - MLD LMS / formation", 1760, 1100, lms_nodes, lms_edges),
        Diagram("schema-mld-communication-vitrine", "MLD Communication", "MangoTaika - MLD communication et vitrine publique", 1640, 860, communication_nodes, communication_edges),
    ]


def main():
    DOCS.mkdir(parents=True, exist_ok=True)
    diagrams = make_diagrams()
    tree = build_drawio(diagrams)
    tree.write(DOCS / "schema-mcd-mld.drawio", encoding="utf-8", xml_declaration=True)
    for diagram in diagrams:
        write_svg(diagram)
    print("Generated aligned draw.io and SVG schema files in docs/")


if __name__ == "__main__":
    main()
