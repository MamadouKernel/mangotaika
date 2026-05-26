using System.Text;

namespace MangoTaika.Helpers;

public static class SimplePdfBuilder
{
    public static byte[] BuildTextPdf(string title, IEnumerable<string> lines)
    {
        var contentLines = new List<string> { title, string.Empty };
        contentLines.AddRange(lines);

        var streamBuilder = new StringBuilder();
        streamBuilder.AppendLine("BT");
        streamBuilder.AppendLine("/F1 14 Tf");
        streamBuilder.AppendLine("50 790 Td");
        streamBuilder.AppendLine($"({Escape(title)}) Tj");
        streamBuilder.AppendLine("/F1 10 Tf");

        var yOffset = 28;
        foreach (var line in contentLines.Skip(2))
        {
            streamBuilder.AppendLine($"0 -{yOffset} Td");
            streamBuilder.AppendLine($"({Escape(line)}) Tj");
            yOffset = 16;
        }

        streamBuilder.AppendLine("ET");

        var stream = Encoding.ASCII.GetBytes(streamBuilder.ToString());
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {stream.Length} >>\nstream\n{Encoding.ASCII.GetString(stream)}endstream"
        };

        var pdf = new StringBuilder();
        pdf.AppendLine("%PDF-1.4");
        var offsets = new List<int> { 0 };

        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(pdf.ToString()));
            pdf.AppendLine($"{i + 1} 0 obj");
            pdf.AppendLine(objects[i]);
            pdf.AppendLine("endobj");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(pdf.ToString());
        pdf.AppendLine("xref");
        pdf.AppendLine($"0 {objects.Count + 1}");
        pdf.AppendLine("0000000000 65535 f ");
        foreach (var offset in offsets.Skip(1))
        {
            pdf.AppendLine($"{offset:0000000000} 00000 n ");
        }

        pdf.AppendLine("trailer");
        pdf.AppendLine($"<< /Size {objects.Count + 1} /Root 1 0 R >>");
        pdf.AppendLine("startxref");
        pdf.AppendLine(xrefOffset.ToString());
        pdf.AppendLine("%%EOF");

        return Encoding.ASCII.GetBytes(pdf.ToString());
    }

    private static string Escape(string value)
        => Normalize(value)
            .Replace("\\", "\\\\")
            .Replace("(", "\\(")
            .Replace(")", "\\)");

    private static string Normalize(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            builder.Append(c <= 127 ? c : RemoveAccent(c));
        }

        return builder.ToString();
    }

    private static char RemoveAccent(char c) => c switch
    {
        'à' or 'á' or 'â' or 'ä' or 'ã' or 'å' => 'a',
        'À' or 'Á' or 'Â' or 'Ä' or 'Ã' or 'Å' => 'A',
        'ç' => 'c',
        'Ç' => 'C',
        'è' or 'é' or 'ê' or 'ë' => 'e',
        'È' or 'É' or 'Ê' or 'Ë' => 'E',
        'ì' or 'í' or 'î' or 'ï' => 'i',
        'Ì' or 'Í' or 'Î' or 'Ï' => 'I',
        'ò' or 'ó' or 'ô' or 'ö' or 'õ' => 'o',
        'Ò' or 'Ó' or 'Ô' or 'Ö' or 'Õ' => 'O',
        'ù' or 'ú' or 'û' or 'ü' => 'u',
        'Ù' or 'Ú' or 'Û' or 'Ü' => 'U',
        'ñ' => 'n',
        'Ñ' => 'N',
        _ => '?'
    };
}
