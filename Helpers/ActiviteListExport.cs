using ClosedXML.Excel;
using MangoTaika.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace MangoTaika.Helpers;

/// <summary>
/// Export serveur de la liste des activites (PDF via QuestPDF, Excel via ClosedXML).
/// Contrairement a l'export client (qui ne lit que les cartes visibles de la page courante),
/// cet export porte sur l'integralite des activites correspondant au filtre de statut.
/// </summary>
public static class ActiviteListExport
{
    private const string Primary = "#597537";
    private const string Dark = "#293a42";
    private const string HeaderBg = "#eef3e6";
    private const string Border = "#cdd5dd";
    private const string Muted = "#667085";

    private static readonly string[] Entetes =
        ["Date", "Titre", "Type", "Groupe", "Lieu", "Participants", "Statut"];

    public static byte[] BuildPdf(IReadOnlyList<ActiviteDto> activites, string libelleStatut)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(28);
                page.DefaultTextStyle(t => t.FontSize(9).FontColor(Dark));

                page.Header().Column(col =>
                {
                    col.Item().Text(PlatformBranding.DistrictName).FontSize(15).Bold().FontColor(Primary);
                    col.Item().Text($"Liste des activites — {libelleStatut}").FontSize(11).FontColor(Dark);
                    col.Item().Text($"{activites.Count} activite(s) — extrait le {DateTime.Now:dd/MM/yyyy HH:mm}")
                        .FontSize(8).FontColor(Muted);
                });

                page.Content().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(78);   // Date
                        columns.RelativeColumn(3);     // Titre
                        columns.RelativeColumn(2);     // Type
                        columns.RelativeColumn(2);     // Groupe
                        columns.RelativeColumn(2);     // Lieu
                        columns.ConstantColumn(58);    // Participants
                        columns.RelativeColumn(1.4f);  // Statut
                    });

                    table.Header(header =>
                    {
                        foreach (var entete in Entetes)
                        {
                            header.Cell().Border(1).BorderColor(Border).Background(HeaderBg).Padding(5)
                                .Text(entete).Bold().FontSize(9).FontColor(Primary);
                        }
                    });

                    foreach (var a in activites)
                    {
                        void BodyCell(string text) =>
                            table.Cell().Border(1).BorderColor(Border).Padding(5).Text(text).FontSize(8);

                        BodyCell(a.DateDebut.ToString("dd/MM/yyyy HH:mm"));
                        BodyCell(a.Titre);
                        BodyCell(a.Type.ToString());
                        BodyCell(a.NomGroupe ?? "Tout le district");
                        BodyCell(string.IsNullOrWhiteSpace(a.Lieu) ? "—" : a.Lieu);
                        BodyCell(a.NbParticipants.ToString());
                        BodyCell(a.Statut.ToString());
                    }
                });

                page.Footer().PaddingTop(6).BorderTop(1).BorderColor(Border).PaddingTop(4)
                    .Row(row =>
                    {
                        row.RelativeItem().Text("Document genere automatiquement depuis la liste des activites.")
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

    public static byte[] BuildExcel(IReadOnlyList<ActiviteDto> activites, string libelleStatut)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Activites");

        ws.Cell(1, 1).Value = $"Liste des activites — {libelleStatut}";
        ws.Range(1, 1, 1, Entetes.Length).Merge().Style.Font.SetBold().Font.SetFontSize(13);
        ws.Cell(2, 1).Value = $"{activites.Count} activite(s) — extrait le {DateTime.Now:dd/MM/yyyy HH:mm}";
        ws.Range(2, 1, 2, Entetes.Length).Merge().Style.Font.SetFontColor(XLColor.FromHtml(Muted));

        const int headerRow = 4;
        for (var c = 0; c < Entetes.Length; c++)
        {
            ws.Cell(headerRow, c + 1).Value = Entetes[c];
        }
        var headerRange = ws.Range(headerRow, 1, headerRow, Entetes.Length);
        headerRange.Style.Font.SetBold();
        headerRange.Style.Fill.SetBackgroundColor(XLColor.FromHtml(HeaderBg));

        var r = headerRow + 1;
        foreach (var a in activites)
        {
            ws.Cell(r, 1).Value = a.DateDebut;
            ws.Cell(r, 1).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
            ws.Cell(r, 2).Value = a.Titre;
            ws.Cell(r, 3).Value = a.Type.ToString();
            ws.Cell(r, 4).Value = a.NomGroupe ?? "Tout le district";
            ws.Cell(r, 5).Value = string.IsNullOrWhiteSpace(a.Lieu) ? "—" : a.Lieu;
            ws.Cell(r, 6).Value = a.NbParticipants;
            ws.Cell(r, 7).Value = a.Statut.ToString();
            r++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
