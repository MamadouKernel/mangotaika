namespace MangoTaika.DTOs;

public class InscriptionAnnuelleImportResultDto
{
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> CreatedEntries { get; set; } = [];
    public List<string> UpdatedEntries { get; set; } = [];
    public List<InscriptionAnnuelleImportErrorDto> Errors { get; set; } = [];
}

public class InscriptionAnnuelleImportErrorDto
{
    public int LineNumber { get; set; }
    public string? ScoutLabel { get; set; }
    public string Message { get; set; } = string.Empty;

    public string DisplayMessage => string.IsNullOrWhiteSpace(ScoutLabel)
        ? $"Ligne {LineNumber}: {Message}"
        : $"Ligne {LineNumber} ({ScoutLabel}): {Message}";
}
