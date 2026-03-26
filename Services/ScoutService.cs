using ClosedXML.Excel;
using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace MangoTaika.Services;

public class ScoutService(AppDbContext db) : IScoutService
{
    public async Task<List<ScoutDto>> GetAllAsync()
    {
        return await db.Scouts
            .Include(s => s.Groupe)
            .Include(s => s.Branche)
            .Where(s => s.IsActive)
            .Select(s => ToDto(s))
            .ToListAsync();
    }

    public async Task<ScoutDto?> GetByIdAsync(Guid id)
    {
        var scout = await db.Scouts
            .Include(s => s.Groupe)
            .Include(s => s.Branche)
            .FirstOrDefaultAsync(s => s.Id == id);
        return scout is null ? null : ToDto(scout);
    }

    public async Task<ScoutDto> CreateAsync(ScoutCreateDto dto)
    {
        var scout = new Scout
        {
            Id = Guid.NewGuid(),
            Matricule = dto.Matricule,
            Nom = dto.Nom,
            Prenom = dto.Prenom,
            DateNaissance = DateTime.SpecifyKind(dto.DateNaissance, DateTimeKind.Utc),
            LieuNaissance = dto.LieuNaissance,
            Sexe = dto.Sexe,
            Telephone = dto.Telephone,
            Email = dto.Email,
            RegionScoute = dto.RegionScoute,
            District = dto.District,
            NumeroCarte = dto.NumeroCarte,
            Fonction = dto.Fonction,
            AssuranceAnnuelle = dto.AssuranceAnnuelle,
            AdresseGeographique = dto.AdresseGeographique,
            GroupeId = dto.GroupeId,
            BrancheId = dto.BrancheId
        };
        db.Scouts.Add(scout);
        await db.SaveChangesAsync();
        return ToDto(scout);
    }

    public async Task<bool> UpdateAsync(Guid id, ScoutCreateDto dto)
    {
        var scout = await db.Scouts.FindAsync(id);
        if (scout is null) return false;

        scout.Matricule = dto.Matricule;
        scout.Nom = dto.Nom;
        scout.Prenom = dto.Prenom;
        scout.DateNaissance = DateTime.SpecifyKind(dto.DateNaissance, DateTimeKind.Utc);
        scout.LieuNaissance = dto.LieuNaissance;
        scout.Sexe = dto.Sexe;
        scout.Telephone = dto.Telephone;
        scout.Email = dto.Email;
        scout.RegionScoute = dto.RegionScoute;
        scout.District = dto.District;
        scout.NumeroCarte = dto.NumeroCarte;
        scout.Fonction = dto.Fonction;
        scout.AssuranceAnnuelle = dto.AssuranceAnnuelle;
        scout.AdresseGeographique = dto.AdresseGeographique;
        scout.GroupeId = dto.GroupeId;
        scout.BrancheId = dto.BrancheId;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var scout = await db.Scouts.FindAsync(id);
        if (scout is null) return false;
        scout.IsActive = false;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<ScoutDto>> SearchAsync(string terme)
    {
        return await db.Scouts
            .Include(s => s.Groupe)
            .Include(s => s.Branche)
            .Where(s => s.Nom.Contains(terme) || s.Prenom.Contains(terme) || s.Matricule.Contains(terme) || (s.NumeroCarte != null && s.NumeroCarte.Contains(terme)) || (s.District != null && s.District.Contains(terme)))
            .Select(s => ToDto(s))
            .ToListAsync();
    }

    public async Task<ScoutImportResultDto> ImportFromExcelAsync(Stream fileStream)
    {
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheets.First();
        var result = new ScoutImportResultDto();

        var headerMap = worksheet.Row(1)
            .CellsUsed()
            .ToDictionary(
                cell => NormalizeHeader(cell.GetString()),
                cell => cell.Address.ColumnNumber,
                StringComparer.OrdinalIgnoreCase);

        var requiredHeaders = new[] { "matricule", "nom", "prenom", "datenaissance" };
        var missingHeaders = requiredHeaders.Where(h => !headerMap.ContainsKey(h)).ToList();
        if (missingHeaders.Count != 0)
        {
            result.Errors.Add(new ScoutImportErrorDto
            {
                LineNumber = 1,
                Message = $"Colonnes obligatoires manquantes: {string.Join(", ", missingHeaders)}."
            });
            return result;
        }

        var groupes = await db.Groupes
            .Where(g => g.IsActive)
            .ToListAsync();
        var branches = await db.Branches
            .Where(b => b.IsActive)
            .ToListAsync();

        var groupesByName = groupes
            .GroupBy(g => NormalizeLookup(g.Nom))
            .ToDictionary(g => g.Key, g => g.First());
        var branchesByName = branches
            .GroupBy(b => NormalizeLookup(b.Nom))
            .ToDictionary(g => g.Key, g => g.First());

        var existingMatricules = await db.Scouts
            .Select(s => s.Matricule.ToUpper())
            .ToHashSetAsync();
        var existingNumeroCartes = await db.Scouts
            .Where(s => s.NumeroCarte != null)
            .Select(s => s.NumeroCarte!.ToUpper())
            .ToHashSetAsync();

        var seenMatricules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenNumeroCartes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var row = worksheet.Row(rowNumber);
            if (row.CellsUsed().All(c => string.IsNullOrWhiteSpace(c.GetString())))
            {
                continue;
            }

            var rowErrors = new List<string>();
            var matricule = ReadString(row, headerMap, "matricule");
            var nom = ReadString(row, headerMap, "nom");
            var prenom = ReadString(row, headerMap, "prenom");
            var lieuNaissance = ReadString(row, headerMap, "lieunaissance");
            var sexe = ReadString(row, headerMap, "sexe");
            var telephone = ReadString(row, headerMap, "telephone");
            var email = ReadString(row, headerMap, "email");
            var regionScoute = ReadString(row, headerMap, "regionscoute");
            var district = ReadString(row, headerMap, "district");
            var numeroCarte = ReadString(row, headerMap, "numerocarte");
            var fonction = ReadString(row, headerMap, "fonction");
            var adresse = ReadString(row, headerMap, "adressegeographique");
            var groupeNom = ReadString(row, headerMap, "groupe");
            var brancheNom = ReadString(row, headerMap, "branche");

            if (string.IsNullOrWhiteSpace(matricule))
            {
                rowErrors.Add("Matricule obligatoire.");
            }
            if (string.IsNullOrWhiteSpace(nom))
            {
                rowErrors.Add("Nom obligatoire.");
            }
            if (string.IsNullOrWhiteSpace(prenom))
            {
                rowErrors.Add("Prenom obligatoire.");
            }

            if (!TryReadDate(row, headerMap, "datenaissance", out var dateNaissance))
            {
                rowErrors.Add("Date de naissance invalide.");
            }

            var assurance = TryReadBoolean(row, headerMap, "assuranceannuelle");

            Groupe? groupe = null;
            if (!string.IsNullOrWhiteSpace(groupeNom))
            {
                if (!groupesByName.TryGetValue(NormalizeLookup(groupeNom), out groupe))
                {
                    rowErrors.Add($"Groupe introuvable: {groupeNom}.");
                }
            }

            Branche? branche = null;
            if (!string.IsNullOrWhiteSpace(brancheNom))
            {
                if (!branchesByName.TryGetValue(NormalizeLookup(brancheNom), out branche))
                {
                    rowErrors.Add($"Branche introuvable: {brancheNom}.");
                }
            }

            if (groupe != null && branche != null && branche.GroupeId != groupe.Id)
            {
                rowErrors.Add("La branche selectionnee n'appartient pas au groupe renseigne.");
            }

            var matriculeKey = (matricule ?? string.Empty).Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(matriculeKey))
            {
                if (existingMatricules.Contains(matriculeKey))
                {
                    rowErrors.Add($"Matricule deja existant: {matricule}.");
                }
                if (!seenMatricules.Add(matriculeKey))
                {
                    rowErrors.Add($"Matricule duplique dans le fichier: {matricule}.");
                }
            }

            var numeroCarteKey = (numeroCarte ?? string.Empty).Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(numeroCarteKey))
            {
                if (existingNumeroCartes.Contains(numeroCarteKey))
                {
                    rowErrors.Add($"Numero de carte deja existant: {numeroCarte}.");
                }
                if (!seenNumeroCartes.Add(numeroCarteKey))
                {
                    rowErrors.Add($"Numero de carte duplique dans le fichier: {numeroCarte}.");
                }
            }

            if (rowErrors.Count != 0)
            {
                result.Errors.Add(new ScoutImportErrorDto
                {
                    LineNumber = rowNumber,
                    Message = string.Join(" ", rowErrors)
                });
                result.SkippedCount++;
                continue;
            }

            db.Scouts.Add(new Scout
            {
                Id = Guid.NewGuid(),
                Matricule = matricule!.Trim(),
                Nom = nom!.Trim(),
                Prenom = prenom!.Trim(),
                DateNaissance = DateTime.SpecifyKind(dateNaissance, DateTimeKind.Utc),
                LieuNaissance = lieuNaissance,
                Sexe = sexe,
                Telephone = telephone,
                Email = email,
                RegionScoute = regionScoute,
                District = district,
                NumeroCarte = numeroCarte,
                Fonction = fonction,
                AssuranceAnnuelle = assurance,
                AdresseGeographique = adresse,
                GroupeId = groupe?.Id,
                BrancheId = branche?.Id
            });

            existingMatricules.Add(matriculeKey);
            if (!string.IsNullOrWhiteSpace(numeroCarteKey))
            {
                existingNumeroCartes.Add(numeroCarteKey);
            }
            result.CreatedCount++;
        }

        if (result.CreatedCount > 0)
        {
            await db.SaveChangesAsync();
        }

        return result;
    }

    public byte[] GenerateImportTemplate()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Scouts");

        var headers = new[]
        {
            "Matricule", "Nom", "Prenom", "DateNaissance", "LieuNaissance", "Sexe",
            "Telephone", "Email", "RegionScoute", "District", "NumeroCarte",
            "Fonction", "AssuranceAnnuelle", "AdresseGeographique", "Groupe", "Branche"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#EAF4D7");
        }

        worksheet.Cell(2, 1).Value = "MT-2026-0101";
        worksheet.Cell(2, 2).Value = "Doe";
        worksheet.Cell(2, 3).Value = "Jean";
        worksheet.Cell(2, 4).Value = new DateTime(2012, 5, 14);
        worksheet.Cell(2, 5).Value = "Abidjan";
        worksheet.Cell(2, 6).Value = "M";
        worksheet.Cell(2, 7).Value = "0700000000";
        worksheet.Cell(2, 8).Value = "jean.doe@email.ci";
        worksheet.Cell(2, 9).Value = "Abidjan";
        worksheet.Cell(2, 10).Value = "District MANGO TAIKA";
        worksheet.Cell(2, 11).Value = "ASCCI-001";
        worksheet.Cell(2, 12).Value = "Scout";
        worksheet.Cell(2, 13).Value = "Oui";
        worksheet.Cell(2, 14).Value = "Cocody Angre";
        worksheet.Cell(2, 15).Value = "Groupe Saint-Michel";
        worksheet.Cell(2, 16).Value = "Louveteaux";
        worksheet.Cell(2, 4).Style.DateFormat.Format = "dd/MM/yyyy";
        worksheet.Columns().AdjustToContents();
        worksheet.SheetView.FreezeRows(1);

        var guide = workbook.Worksheets.Add("Guide");
        guide.Cell("A1").Value = "Colonnes obligatoires";
        guide.Cell("A2").Value = "Matricule, Nom, Prenom, DateNaissance";
        guide.Cell("A4").Value = "Regles";
        guide.Cell("A5").Value = "Groupe et Branche doivent correspondre aux noms deja presents dans l'application.";
        guide.Cell("A6").Value = "AssuranceAnnuelle accepte: Oui, Non, True, False, 1, 0.";
        guide.Cell("A7").Value = "DateNaissance peut etre au format Excel date ou jj/MM/aaaa.";
        guide.Columns().AdjustToContents();

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        return memoryStream.ToArray();
    }

    private static ScoutDto ToDto(Scout s) => new()
    {
        Id = s.Id,
        Matricule = s.Matricule,
        Nom = s.Nom,
        Prenom = s.Prenom,
        DateNaissance = s.DateNaissance,
        Sexe = s.Sexe,
        Telephone = s.Telephone,
        Email = s.Email,
        RegionScoute = s.RegionScoute,
        District = s.District,
        NumeroCarte = s.NumeroCarte,
        Fonction = s.Fonction,
        StatutASCCI = s.StatutASCCI,
        AssuranceAnnuelle = s.AssuranceAnnuelle,
        AdresseGeographique = s.AdresseGeographique,
        GroupeId = s.GroupeId,
        BrancheId = s.BrancheId,
        NomGroupe = s.Groupe?.Nom,
        NomBranche = s.Branche?.Nom
    };

    private static string ReadString(IXLRow row, IDictionary<string, int> headerMap, string header)
    {
        return headerMap.TryGetValue(header, out var column)
            ? row.Cell(column).GetString().Trim()
            : string.Empty;
    }

    private static bool TryReadDate(IXLRow row, IDictionary<string, int> headerMap, string header, out DateTime date)
    {
        date = default;
        if (!headerMap.TryGetValue(header, out var column))
        {
            return false;
        }

        var cell = row.Cell(column);
        if (cell.IsEmpty())
        {
            return false;
        }

        if (cell.DataType == XLDataType.DateTime)
        {
            date = cell.GetDateTime().Date;
            return true;
        }

        var raw = cell.GetString().Trim();
        return DateTime.TryParse(raw, CultureInfo.GetCultureInfo("fr-FR"), DateTimeStyles.None, out date)
            || DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    private static bool TryReadBoolean(IXLRow row, IDictionary<string, int> headerMap, string header)
    {
        var raw = ReadString(row, headerMap, header);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        return raw.Trim().ToLowerInvariant() switch
        {
            "oui" => true,
            "true" => true,
            "1" => true,
            "x" => true,
            "yes" => true,
            _ => false
        };
    }

    private static string NormalizeHeader(string value)
    {
        return new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    }

    private static string NormalizeLookup(string? value)
    {
        return (value ?? string.Empty).Trim().ToUpperInvariant();
    }
}
