using System.Net;
using System.Text;
using MangoTaika.Data.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MangoTaika.Helpers;

/// <summary>
/// Construit le rapport d'activite a partir d'un modele de contenu unique, puis le rend
/// en PDF (QuestPDF) et en Word (HTML) avec une mise en page identique : meme en-tete avec
/// logo du groupe, memes sections, meme tableau de participants.
/// </summary>
public static class RapportActiviteExport
{
    // Palette de marque partagee entre les deux formats.
    private const string Primary = "#597537";
    private const string Dark = "#293a42";
    private const string HeaderBg = "#eef3e6";
    private const string Border = "#cdd5dd";
    private const string Muted = "#667085";

    public static byte[] BuildPdf(RapportActivite rapport, byte[]? logoBytes)
    {
        var content = BuildContent(rapport);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(t => t.FontSize(10).FontColor(Dark));

                page.Header().Element(h => ComposeHeader(h, content, logoBytes));
                page.Content().PaddingTop(14).Element(c => ComposeBody(c, content));
                page.Footer().PaddingTop(8).BorderTop(1).BorderColor(Border).PaddingTop(6)
                    .Row(row =>
                    {
                        row.RelativeItem().Text("Document genere automatiquement depuis le rapport d'activite.")
                            .FontSize(8).FontColor(Muted);
                        row.ConstantItem(120).AlignRight().Text(t =>
                        {
                            t.Span("Page ").FontSize(8).FontColor(Muted);
                            t.CurrentPageNumber().FontSize(8).FontColor(Muted);
                            t.Span(" / ").FontSize(8).FontColor(Muted);
                            t.TotalPages().FontSize(8).FontColor(Muted);
                        });
                    });
            });
        }).GeneratePdf();
    }

    public static string BuildWordHtml(RapportActivite rapport, byte[]? logoBytes, string? logoMime)
    {
        var content = BuildContent(rapport);
        var logoTag = BuildLogoImgTag(logoBytes, logoMime);

        var sb = new StringBuilder();
        sb.Append("<!doctype html><html><head><meta charset=\"utf-8\"><title>Rapport - ")
          .Append(Enc(content.Meta.Activite)).Append("</title>");
        sb.Append("<style>")
          .Append("body{font-family:Arial,sans-serif;color:").Append(Dark).Append(";line-height:1.45;}")
          .Append(".header{border-bottom:3px solid ").Append(Primary).Append(";padding-bottom:14px;margin-bottom:18px;}")
          .Append(".header-table{width:100%;border-collapse:collapse;border:0;}")
          .Append(".header-table td{border:0;vertical-align:middle;padding:0;}")
          .Append(".logo{width:64px;height:64px;object-fit:contain;}")
          .Append(".brand{color:").Append(Primary).Append(";font-size:16px;font-weight:700;letter-spacing:1px;}")
          .Append(".brand-sub{color:").Append(Muted).Append(";font-size:11px;}")
          .Append("h1{color:").Append(Dark).Append(";font-size:20px;margin:6px 0 0;}")
          .Append("h2{color:").Append(Primary).Append(";font-size:13px;margin-top:16px;border-bottom:1px solid #d9e2d0;padding-bottom:4px;text-transform:uppercase;letter-spacing:.5px;}")
          .Append("p,div{font-size:12px;}")
          .Append(".meta-table{width:100%;border-collapse:collapse;margin-top:6px;font-size:12px;}")
          .Append(".meta-table td{border:1px solid ").Append(Border).Append(";padding:5px 8px;}")
          .Append(".meta-table td.k{background:").Append(HeaderBg).Append(";color:").Append(Dark).Append(";font-weight:600;width:190px;}")
          .Append("table.list{width:100%;border-collapse:collapse;font-size:11px;margin-top:8px;}")
          .Append("table.list th,table.list td{border:1px solid ").Append(Border).Append(";padding:6px 8px;text-align:left;}")
          .Append("table.list th{background:").Append(HeaderBg).Append(";color:").Append(Dark).Append(";}")
          .Append(".footer{margin-top:28px;padding-top:12px;border-top:1px solid #d9e2d0;color:").Append(Muted).Append(";font-size:11px;}")
          .Append("</style></head><body>");

        // En-tete : logo + marque + titre.
        sb.Append("<div class=\"header\"><table class=\"header-table\"><tr>");
        if (!string.IsNullOrEmpty(logoTag))
        {
            sb.Append("<td style=\"width:72px;\">").Append(logoTag).Append("</td>");
        }
        sb.Append("<td><div class=\"brand\">").Append(Enc(PlatformBranding.DistrictName))
          .Append("</div><div class=\"brand-sub\">").Append(Enc(PlatformBranding.DistrictLabel))
          .Append("</div><h1>Rapport d'activite</h1></td></tr></table></div>");

        // Tableau des metadonnees.
        sb.Append("<table class=\"meta-table\">");
        AppendMetaRow(sb, "Activite", content.Meta.Activite);
        AppendMetaRow(sb, "Groupe", content.Meta.Groupe);
        AppendMetaRow(sb, "Lieu", content.Meta.Lieu);
        AppendMetaRow(sb, "Dates", content.Meta.Dates);
        AppendMetaRow(sb, "Date de realisation", content.Meta.Realisation);
        AppendMetaRow(sb, "Participants", content.Meta.Participants.ToString());
        AppendMetaRow(sb, "Statut", content.Meta.Statut);
        AppendMetaRow(sb, "Cree par", content.Meta.CreePar);
        if (!string.IsNullOrEmpty(content.Meta.ValidePar))
        {
            AppendMetaRow(sb, "Valide par", content.Meta.ValidePar!);
        }
        sb.Append("</table>");

        // Sections texte.
        foreach (var (title, body) in content.Sections)
        {
            sb.Append("<h2>").Append(Enc(title)).Append("</h2><div>")
              .Append(Enc(body).Replace("\r\n", "\n").Replace("\n", "<br />")).Append("</div>");
        }

        // Tableau des participants.
        sb.Append("<h2>Liste des participants (").Append(content.ParticipantCount).Append(")</h2>");
        if (content.Participants.Count == 0)
        {
            sb.Append("<p>Aucun participant enregistre.</p>");
        }
        else
        {
            sb.Append("<table class=\"list\"><thead><tr><th>#</th><th>Nom et prenom</th><th>Matricule / Type</th><th>Categorie</th><th>Fonction</th><th>Presence</th></tr></thead><tbody>");
            foreach (var p in content.Participants)
            {
                sb.Append("<tr><td>").Append(p.Index).Append("</td><td>").Append(Enc(p.Nom))
                  .Append("</td><td>").Append(Enc(p.Complement)).Append("</td><td>").Append(Enc(p.Categorie))
                  .Append("</td><td>").Append(Enc(p.Fonction)).Append("</td><td>").Append(Enc(p.Presence))
                  .Append("</td></tr>");
            }
            sb.Append("</tbody></table>");
        }

        if (content.PiecesJointes.Count != 0)
        {
            sb.Append("<h2>Pieces jointes</h2><ul>");
            foreach (var piece in content.PiecesJointes)
            {
                sb.Append("<li>").Append(Enc(piece)).Append("</li>");
            }
            sb.Append("</ul>");
        }

        sb.Append("<div class=\"footer\">Document genere automatiquement depuis le rapport d'activite.</div>");
        sb.Append("</body></html>");
        return sb.ToString();
    }

    // ---- Rendu PDF (QuestPDF) ----

    private static void ComposeHeader(IContainer container, RapportContent content, byte[]? logoBytes)
    {
        container.BorderBottom(3).BorderColor(Primary).PaddingBottom(10).Row(row =>
        {
            if (logoBytes is { Length: > 0 })
            {
                row.ConstantItem(64).Height(64).AlignMiddle().Image(logoBytes).FitArea();
                row.ConstantItem(12);
            }

            row.RelativeItem().Column(col =>
            {
                col.Item().Text(PlatformBranding.DistrictName).FontSize(15).Bold().FontColor(Primary);
                col.Item().Text(PlatformBranding.DistrictLabel).FontSize(10).FontColor(Muted);
                col.Item().PaddingTop(4).Text("Rapport d'activite").FontSize(18).Bold().FontColor(Dark);
            });
        });
    }

    private static void ComposeBody(IContainer container, RapportContent content)
    {
        container.Column(col =>
        {
            col.Spacing(0);

            // Tableau des metadonnees.
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(170);
                    c.RelativeColumn();
                });

                void MetaRow(string key, string value)
                {
                    table.Cell().Border(1).BorderColor(Border).Background(HeaderBg).Padding(5)
                        .Text(key).Bold().FontColor(Dark);
                    table.Cell().Border(1).BorderColor(Border).Padding(5).Text(value);
                }

                MetaRow("Activite", content.Meta.Activite);
                MetaRow("Groupe", content.Meta.Groupe);
                MetaRow("Lieu", content.Meta.Lieu);
                MetaRow("Dates", content.Meta.Dates);
                MetaRow("Date de realisation", content.Meta.Realisation);
                MetaRow("Participants", content.Meta.Participants.ToString());
                MetaRow("Statut", content.Meta.Statut);
                MetaRow("Cree par", content.Meta.CreePar);
                if (!string.IsNullOrEmpty(content.Meta.ValidePar))
                {
                    MetaRow("Valide par", content.Meta.ValidePar!);
                }
            });

            // Sections texte.
            foreach (var (title, body) in content.Sections)
            {
                col.Item().PaddingTop(14).Element(e => SectionTitle(e, title));
                col.Item().PaddingTop(4).Text(body).FontSize(11);
            }

            // Tableau des participants.
            col.Item().PaddingTop(14).Element(e => SectionTitle(e, $"Liste des participants ({content.ParticipantCount})"));
            if (content.Participants.Count == 0)
            {
                col.Item().PaddingTop(4).Text("Aucun participant enregistre.").FontSize(11);
            }
            else
            {
                col.Item().PaddingTop(6).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(24);
                        c.RelativeColumn(3);
                        c.RelativeColumn(2);
                        c.RelativeColumn(1.4f);
                        c.RelativeColumn(2);
                        c.RelativeColumn(1.4f);
                    });

                    table.Header(header =>
                    {
                        void HeadCell(string text) =>
                            header.Cell().Border(1).BorderColor(Border).Background(HeaderBg).Padding(5)
                                .Text(text).Bold().FontSize(9).FontColor(Dark);

                        HeadCell("#");
                        HeadCell("Nom et prenom");
                        HeadCell("Matricule / Type");
                        HeadCell("Categorie");
                        HeadCell("Fonction");
                        HeadCell("Presence");
                    });

                    foreach (var p in content.Participants)
                    {
                        void BodyCell(string text) =>
                            table.Cell().Border(1).BorderColor(Border).Padding(5).Text(text).FontSize(9);

                        BodyCell(p.Index.ToString());
                        BodyCell(p.Nom);
                        BodyCell(p.Complement);
                        BodyCell(p.Categorie);
                        BodyCell(p.Fonction);
                        BodyCell(p.Presence);
                    }
                });
            }

            // Pieces jointes.
            if (content.PiecesJointes.Count != 0)
            {
                col.Item().PaddingTop(14).Element(e => SectionTitle(e, "Pieces jointes"));
                foreach (var piece in content.PiecesJointes)
                {
                    col.Item().PaddingTop(2).Text("• " + piece).FontSize(11);
                }
            }
        });
    }

    private static void SectionTitle(IContainer container, string text)
    {
        container.BorderBottom(1).BorderColor("#d9e2d0").PaddingBottom(3)
            .Text(text.ToUpperInvariant()).FontSize(12).Bold().FontColor(Primary);
    }

    // ---- Modele de contenu partage ----

    private static RapportContent BuildContent(RapportActivite r)
    {
        var dates = $"{r.Activite.DateDebut:dd/MM/yyyy}" +
                    (r.Activite.DateFin.HasValue ? $" au {r.Activite.DateFin:dd/MM/yyyy}" : string.Empty);

        var meta = new RapportMeta(
            Activite: r.Activite.Titre,
            Groupe: r.Activite.Groupe?.Nom ?? "District",
            Lieu: r.Activite.Lieu ?? "Non precise",
            Dates: dates,
            Realisation: r.DateRealisation.ToString("dd/MM/yyyy"),
            Participants: r.NombreParticipants,
            Statut: r.Statut.ToString(),
            CreePar: $"{PersonLabel(r.Createur)} le {r.DateCreation:dd/MM/yyyy}",
            ValidePar: r.Valideur is not null && r.DateValidation.HasValue
                ? $"{PersonLabel(r.Valideur)} le {r.DateValidation:dd/MM/yyyy}"
                : null);

        var sections = new List<(string, string)>
        {
            ("1. Resume executif", BlockOrDefault(r.ResumeExecutif)),
            ("2. Resultats obtenus", BlockOrDefault(r.ResultatsObtenus)),
            ("3. Difficultes rencontrees", BlockOrDefault(r.DifficultesRencontrees)),
            ("4. Recommandations", BlockOrDefault(r.Recommandations)),
        };
        if (!string.IsNullOrWhiteSpace(r.ObservationsComplementaires))
        {
            sections.Add(("5. Observations complementaires", r.ObservationsComplementaires.Trim()));
        }

        var participants = r.Activite.Participants
            .OrderBy(p => p.Scout?.Nom ?? p.Ressource?.Nom ?? string.Empty)
            .ThenBy(p => p.Scout?.Prenom ?? p.Ressource?.Prenom ?? string.Empty)
            .ToList();

        var rows = new List<ParticipantRow>();
        int idx = 1;
        foreach (var p in participants)
        {
            string nom, complement, categorie, fonction;
            if (p.Scout is not null)
            {
                nom = $"{p.Scout.Nom} {p.Scout.Prenom}".Trim();
                complement = string.IsNullOrWhiteSpace(p.Scout.Matricule) ? "-" : p.Scout.Matricule!;
                categorie = "Scout";
                fonction = string.IsNullOrWhiteSpace(p.Scout.Fonction) ? "-" : p.Scout.Fonction!;
            }
            else if (p.Ressource is not null)
            {
                nom = $"{p.Ressource.Nom} {p.Ressource.Prenom}".Trim();
                complement = p.Ressource.Type.ToString();
                categorie = "Ressource";
                fonction = p.Ressource.Type.ToString();
            }
            else
            {
                nom = "Participant inconnu";
                complement = "-";
                categorie = "-";
                fonction = "-";
            }

            rows.Add(new ParticipantRow(idx++, nom, complement, categorie, fonction, p.Presence.ToString()));
        }

        var pieces = r.PiecesJointes.Select(p => p.NomFichier).ToList();

        return new RapportContent(meta, sections, rows, pieces, r.Activite.Participants.Count);
    }

    private static string BlockOrDefault(string? text)
        => string.IsNullOrWhiteSpace(text) ? "(Non renseigne)" : text.Trim();

    private static string PersonLabel(ApplicationUser? user)
        => user is null ? "Inconnu" : $"{user.Prenom} {user.Nom}".Trim();

    private static string Enc(string value) => WebUtility.HtmlEncode(value);

    private static void AppendMetaRow(StringBuilder sb, string key, string value)
        => sb.Append("<tr><td class=\"k\">").Append(Enc(key)).Append("</td><td>").Append(Enc(value)).Append("</td></tr>");

    private static string BuildLogoImgTag(byte[]? logoBytes, string? logoMime)
    {
        if (logoBytes is not { Length: > 0 })
        {
            return string.Empty;
        }

        var mime = string.IsNullOrWhiteSpace(logoMime) ? "image/png" : logoMime;
        var base64 = Convert.ToBase64String(logoBytes);
        return $"<img class=\"logo\" src=\"data:{mime};base64,{base64}\" alt=\"Logo\" />";
    }

    private sealed record RapportMeta(
        string Activite, string Groupe, string Lieu, string Dates, string Realisation,
        int Participants, string Statut, string CreePar, string? ValidePar);

    private sealed record ParticipantRow(
        int Index, string Nom, string Complement, string Categorie, string Fonction, string Presence);

    private sealed record RapportContent(
        RapportMeta Meta,
        IReadOnlyList<(string Title, string Body)> Sections,
        IReadOnlyList<ParticipantRow> Participants,
        IReadOnlyList<string> PiecesJointes,
        int ParticipantCount);
}
