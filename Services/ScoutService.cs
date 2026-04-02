using ClosedXML.Excel;
using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
using MangoTaika.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace MangoTaika.Services;

public class ScoutService(AppDbContext db) : IScoutService
{
    private const string InvalidWorkbookMessage =
        "Le fichier importe n'est pas un fichier Excel (.xlsx) valide ou il est endommage. Telechargez le modele Excel, remplissez-le puis reessayez.";
    private const string ImportPersistenceErrorMessage =
        "L'import a rencontre une erreur pendant l'enregistrement en base. Verifiez les doublons de matricule, de numero de carte et la correspondance groupe/branche.";

    public async Task<List<ScoutDto>> GetAllAsync()
    {
        var scouts = await db.Scouts
            .Include(s => s.Groupe)
            .Include(s => s.Branche)
            .Where(s => s.IsActive)
            .ToListAsync();

        var dtos = scouts.Select(ToDto).ToList();
        await PopulateHistoryAsync(dtos);
        return dtos;
    }

    public async Task<ScoutDto?> GetByIdAsync(Guid id)
    {
        var scout = await db.Scouts
            .Include(s => s.Groupe)
            .Include(s => s.Branche)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (scout is null)
        {
            return null;
        }

        var dto = ToDto(scout);
        await PopulateHistoryAsync([dto]);
        return dto;
    }

    public async Task<ScoutDto> CreateAsync(ScoutCreateDto dto)
    {
        var requestedMatricule = ScoutMatriculeFormat.NormalizeOptional(dto.Matricule);
        var nom = NormalizeRequired(dto.Nom, "Le nom est requis.");
        var prenom = NormalizeRequired(dto.Prenom, "Le prenom est requis.");
        var dateNaissance = NormalizeDateNaissance(dto.DateNaissance);
        var numeroCarte = NormalizeOptional(dto.NumeroCarte);
        ValidateManualMatriculeInput(requestedMatricule);
        await EnsureUniqueNumeroCarteAsync(numeroCarte);
        await ValidateAffectationAsync(dto.GroupeId, dto.BrancheId);

        var scout = new Scout
        {
            Id = Guid.NewGuid(),
            Matricule = null,
            Nom = nom,
            Prenom = prenom,
            DateNaissance = DateTime.SpecifyKind(dateNaissance, DateTimeKind.Utc),
            LieuNaissance = NormalizeOptional(dto.LieuNaissance),
            Sexe = NormalizeOptional(dto.Sexe),
            Telephone = NormalizeOptional(dto.Telephone),
            Email = NormalizeOptional(dto.Email),
            RegionScoute = NormalizeOptional(dto.RegionScoute),
            District = NormalizeOptional(dto.District),
            NumeroCarte = numeroCarte,
            Fonction = NormalizeOptional(dto.Fonction),
            FonctionVieActive = NormalizeOptional(dto.FonctionVieActive),
            NiveauFormationScoute = NormalizeOptional(dto.NiveauFormationScoute),
            ContactUrgenceNom = NormalizeOptional(dto.ContactUrgenceNom),
            ContactUrgenceRelation = NormalizeOptional(dto.ContactUrgenceRelation),
            ContactUrgenceTelephone = NormalizeOptional(dto.ContactUrgenceTelephone),
            PhotoUrl = NormalizeOptional(dto.PhotoUrl),
            AssuranceAnnuelle = false,
            AdresseGeographique = NormalizeOptional(dto.AdresseGeographique),
            GroupeId = dto.GroupeId,
            BrancheId = dto.BrancheId
        };
        db.Scouts.Add(scout);
        await SaveChangesAsync();
        return ToDto(scout);
    }

    public async Task<bool> UpdateAsync(Guid id, ScoutCreateDto dto)
    {
        var scout = await db.Scouts.FindAsync(id);
        if (scout is null) return false;

        var requestedMatricule = ScoutMatriculeFormat.NormalizeOptional(dto.Matricule);
        var nom = NormalizeRequired(dto.Nom, "Le nom est requis.");
        var prenom = NormalizeRequired(dto.Prenom, "Le prenom est requis.");
        var dateNaissance = NormalizeDateNaissance(dto.DateNaissance);
        var numeroCarte = NormalizeOptional(dto.NumeroCarte);
        ValidateManualMatriculeInput(requestedMatricule, scout.Matricule);
        await EnsureUniqueNumeroCarteAsync(numeroCarte, id);
        await ValidateAffectationAsync(dto.GroupeId, dto.BrancheId);

        scout.Matricule = ScoutMatriculeFormat.NormalizeOptional(scout.Matricule);
        scout.Nom = nom;
        scout.Prenom = prenom;
        scout.DateNaissance = DateTime.SpecifyKind(dateNaissance, DateTimeKind.Utc);
        scout.LieuNaissance = NormalizeOptional(dto.LieuNaissance);
        scout.Sexe = NormalizeOptional(dto.Sexe);
        scout.Telephone = NormalizeOptional(dto.Telephone);
        scout.Email = NormalizeOptional(dto.Email);
        scout.RegionScoute = NormalizeOptional(dto.RegionScoute);
        scout.District = NormalizeOptional(dto.District);
        scout.NumeroCarte = numeroCarte;
        scout.Fonction = NormalizeOptional(dto.Fonction);
        scout.FonctionVieActive = NormalizeOptional(dto.FonctionVieActive);
        scout.NiveauFormationScoute = NormalizeOptional(dto.NiveauFormationScoute);
        scout.ContactUrgenceNom = NormalizeOptional(dto.ContactUrgenceNom);
        scout.ContactUrgenceRelation = NormalizeOptional(dto.ContactUrgenceRelation);
        scout.ContactUrgenceTelephone = NormalizeOptional(dto.ContactUrgenceTelephone);
        scout.PhotoUrl = NormalizeOptional(dto.PhotoUrl);
        scout.AdresseGeographique = NormalizeOptional(dto.AdresseGeographique);
        scout.GroupeId = dto.GroupeId;
        scout.BrancheId = dto.BrancheId;
        await SaveChangesAsync();
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
        var query = db.Scouts
            .Include(s => s.Groupe)
            .Include(s => s.Branche)
            .Where(s => s.IsActive)
            .AsQueryable();

        List<Scout> scouts;
        if (db.Database.IsNpgsql())
        {
            var pattern = DatabaseText.ToNormalizedContainsPattern(terme);
            scouts = await query.Where(s =>
                    EF.Functions.Like(PostgresTextFunctions.NormalizeSearch(s.Nom), pattern) ||
                    EF.Functions.Like(PostgresTextFunctions.NormalizeSearch(s.Prenom), pattern) ||
                    EF.Functions.Like(PostgresTextFunctions.NormalizeSearch(s.Matricule), pattern) ||
                    (s.NumeroCarte != null && EF.Functions.Like(PostgresTextFunctions.NormalizeSearch(s.NumeroCarte), pattern)) ||
                    (s.District != null && EF.Functions.Like(PostgresTextFunctions.NormalizeSearch(s.District), pattern)))
                .ToListAsync();
        }
        else
        {
            var normalizedTerm = DatabaseText.NormalizeSearchKey(terme);
            scouts = (await query.ToListAsync())
                .Where(s =>
                    DatabaseText.ContainsNormalized(s.Nom, normalizedTerm) ||
                    DatabaseText.ContainsNormalized(s.Prenom, normalizedTerm) ||
                    DatabaseText.ContainsNormalized(s.Matricule, normalizedTerm) ||
                    DatabaseText.ContainsNormalized(s.NumeroCarte, normalizedTerm) ||
                    DatabaseText.ContainsNormalized(s.District, normalizedTerm))
                .ToList();
        }

        var dtos = scouts.Select(ToDto).ToList();
        await PopulateHistoryAsync(dtos);
        return dtos;
    }

    public async Task<ScoutImportResultDto> ImportFromExcelAsync(Stream fileStream)
    {
        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(fileStream);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(InvalidWorkbookMessage, ex);
        }

        using (workbook)
        {
            IXLWorksheet worksheet;
            try
            {
                worksheet = workbook.Worksheets.First();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(InvalidWorkbookMessage, ex);
            }

            var result = new ScoutImportResultDto();

            var headerMap = worksheet.Row(1)
                .CellsUsed()
                .ToDictionary(
                    cell => NormalizeHeader(cell.GetString()),
                    cell => cell.Address.ColumnNumber,
                    StringComparer.OrdinalIgnoreCase);

            var requiredHeaders = new[] { "nom", "prenom", "datenaissance" };
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
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var branchesByGroupAndName = branches
                .GroupBy(b => BuildBranchLookupKey(b.GroupeId, b.Nom))
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var branchesById = branches.ToDictionary(b => b.Id);

            var existingScouts = await db.Scouts.ToListAsync();
            var existingScoutsByMatricule = existingScouts
                .Where(s => !string.IsNullOrWhiteSpace(s.Matricule))
                .GroupBy(s => NormalizeLookup(s.Matricule))
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var existingScoutsByNumeroCarte = existingScouts
                .Where(s => !string.IsNullOrWhiteSpace(s.NumeroCarte))
                .GroupBy(s => NormalizeLookup(s.NumeroCarte))
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var existingScoutsByIdentity = existingScouts
                .GroupBy(s => BuildScoutIdentityKey(s.Nom, s.Prenom, s.DateNaissance))
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var seenMatricules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenNumeroCartes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenIdentityKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
            for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
            {
                var row = worksheet.Row(rowNumber);
                if (row.CellsUsed().All(c => string.IsNullOrWhiteSpace(c.GetString())))
                {
                    continue;
                }

                var rowErrors = new List<string>();
                var matricule = ScoutMatriculeFormat.NormalizeOptional(ReadString(row, headerMap, "matricule"));
                var nom = ReadString(row, headerMap, "nom");
                var prenom = ReadString(row, headerMap, "prenom");
                var lieuNaissance = NormalizeOptional(ReadString(row, headerMap, "lieunaissance"));
                var sexe = NormalizeOptional(ReadString(row, headerMap, "sexe"));
                var telephone = NormalizeOptional(ReadString(row, headerMap, "telephone"));
                var email = NormalizeOptional(ReadString(row, headerMap, "email"));
                var regionScoute = NormalizeOptional(ReadString(row, headerMap, "regionscoute"));
                var district = NormalizeOptional(ReadString(row, headerMap, "district"));
                var numeroCarte = NormalizeOptional(ReadString(row, headerMap, "numerocarte"));
                var fonction = NormalizeOptional(ReadString(row, headerMap, "fonction"));
                var adresse = NormalizeOptional(ReadString(row, headerMap, "adressegeographique"));
                var groupeNom = NormalizeOptional(ReadString(row, headerMap, "groupe"));
                var brancheNom = NormalizeOptional(ReadString(row, headerMap, "branche"));

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

                var identityKey = string.Empty;
                if (rowErrors.Count == 0)
                {
                    identityKey = BuildScoutIdentityKey(nom!, prenom!, dateNaissance);
                    if (!seenIdentityKeys.Add(identityKey))
                    {
                        rowErrors.Add("Scout duplique dans le fichier pour la meme identite.");
                    }
                }

                Scout? existingScout = null;
                var matchedByMatricule = false;
                var matriculeKey = NormalizeLookup(matricule);

                if (!string.IsNullOrWhiteSpace(matricule))
                {
                    if (!ScoutMatriculeFormat.IsValid(matricule))
                    {
                        rowErrors.Add($"Matricule invalide: {matricule}. Format attendu: {ScoutMatriculeFormat.Example}.");
                    }
                    else
                    {
                        if (!seenMatricules.Add(matriculeKey))
                        {
                            rowErrors.Add($"Matricule duplique dans le fichier: {matricule}.");
                        }

                        if (existingScoutsByMatricule.TryGetValue(matriculeKey, out var scoutTrouveParMatricule))
                        {
                            existingScout = scoutTrouveParMatricule;
                            matchedByMatricule = true;
                        }
                    }
                }

                if (existingScout is null && !string.IsNullOrWhiteSpace(identityKey))
                {
                    if (existingScoutsByIdentity.TryGetValue(identityKey, out var scoutsMemeIdentite))
                    {
                        if (scoutsMemeIdentite.Count == 1)
                        {
                            existingScout = scoutsMemeIdentite[0];
                        }
                        else if (scoutsMemeIdentite.Count > 1)
                        {
                            rowErrors.Add("Plusieurs scouts existants correspondent a cette identite. Utilisez le matricule deja attribue.");
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(matricule) && !matchedByMatricule)
                {
                    rowErrors.Add("Le matricule ne peut pas etre attribue manuellement. Laissez cette colonne vide tant que la premiere cotisation nationale n'a pas ete importee.");
                }

                Groupe? providedGroupe = null;
                if (!string.IsNullOrWhiteSpace(groupeNom))
                {
                    if (!groupesByName.TryGetValue(NormalizeLookup(groupeNom), out providedGroupe))
                    {
                        rowErrors.Add($"Groupe introuvable: {groupeNom}.");
                    }
                }

                var effectiveGroupe = ResolveImportGroupe(existingScout, providedGroupe);
                var effectiveBranche = ResolveImportBranche(
                    brancheNom,
                    effectiveGroupe,
                    existingScout,
                    branchesByGroupAndName,
                    branchesById,
                    rowErrors);

                var numeroCarteKey = NormalizeLookup(numeroCarte);
                if (!string.IsNullOrWhiteSpace(numeroCarteKey))
                {
                    if (!seenNumeroCartes.Add(numeroCarteKey))
                    {
                        rowErrors.Add($"Numero de carte duplique dans le fichier: {numeroCarte}.");
                    }

                    if (existingScoutsByNumeroCarte.TryGetValue(numeroCarteKey, out var scoutAvecNumeroCarte)
                        && scoutAvecNumeroCarte.Id != existingScout?.Id)
                    {
                        rowErrors.Add($"Numero de carte deja existant: {numeroCarte}.");
                    }
                }

                if (rowErrors.Count != 0)
                {
                    result.Errors.Add(new ScoutImportErrorDto
                    {
                        LineNumber = rowNumber,
                        Matricule = matricule,
                        Message = string.Join(" ", rowErrors)
                    });
                    result.SkippedCount++;
                    continue;
                }

                var isUpdate = existingScout is not null;
                var scout = existingScout ?? new Scout
                {
                    Id = Guid.NewGuid()
                };
                var previousNumeroCarteKey = NormalizeLookup(scout.NumeroCarte);
                var previousIdentityKey = BuildScoutIdentityKey(scout.Nom, scout.Prenom, scout.DateNaissance);
                var snapshot = isUpdate ? CaptureImportSnapshot(scout) : null;

                ApplyImportValuesToScout(
                    scout,
                    new ScoutImportValues
                    {
                        Matricule = existingScout?.Matricule,
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
                        AdresseGeographique = adresse,
                        GroupeId = effectiveGroupe?.Id,
                        BrancheId = effectiveBranche?.Id
                    },
                    preserveExistingOptionalValues: isUpdate);

                if (isUpdate)
                {
                    scout.IsActive = true;
                }
                else
                {
                    db.Scouts.Add(scout);
                }

                var currentNumeroCarteKey = NormalizeLookup(scout.NumeroCarte);
                var currentIdentityKey = BuildScoutIdentityKey(scout.Nom, scout.Prenom, scout.DateNaissance);
                try
                {
                    await SaveChangesAsync();

                    if (!string.IsNullOrWhiteSpace(scout.Matricule))
                    {
                        existingScoutsByMatricule[NormalizeLookup(scout.Matricule)] = scout;
                    }

                    if (!string.IsNullOrWhiteSpace(previousNumeroCarteKey)
                        && !string.Equals(previousNumeroCarteKey, currentNumeroCarteKey, StringComparison.OrdinalIgnoreCase)
                        && existingScoutsByNumeroCarte.TryGetValue(previousNumeroCarteKey, out var previousOwner)
                        && previousOwner.Id == scout.Id)
                    {
                        existingScoutsByNumeroCarte.Remove(previousNumeroCarteKey);
                    }

                    if (!string.IsNullOrWhiteSpace(currentNumeroCarteKey))
                    {
                        existingScoutsByNumeroCarte[currentNumeroCarteKey] = scout;
                    }

                    if (!string.IsNullOrWhiteSpace(previousIdentityKey)
                        && existingScoutsByIdentity.TryGetValue(previousIdentityKey, out var previousIdentityScouts))
                    {
                        previousIdentityScouts.RemoveAll(s => s.Id == scout.Id);
                        if (previousIdentityScouts.Count == 0)
                        {
                            existingScoutsByIdentity.Remove(previousIdentityKey);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(currentIdentityKey))
                    {
                        if (!existingScoutsByIdentity.TryGetValue(currentIdentityKey, out var currentIdentityScouts))
                        {
                            currentIdentityScouts = [];
                            existingScoutsByIdentity[currentIdentityKey] = currentIdentityScouts;
                        }

                        currentIdentityScouts.RemoveAll(s => s.Id == scout.Id);
                        currentIdentityScouts.Add(scout);
                    }

                    if (isUpdate)
                    {
                        result.UpdatedMatricules.Add(BuildScoutImportDisplayLabel(scout));
                        result.UpdatedCount++;
                    }
                    else
                    {
                        result.CreatedMatricules.Add(BuildScoutImportDisplayLabel(scout));
                        result.CreatedCount++;
                    }
                }
                catch (InvalidOperationException ex)
                {
                    if (isUpdate)
                    {
                        RestoreImportSnapshot(scout, snapshot!);
                        db.Entry(scout).State = EntityState.Unchanged;
                    }
                    else
                    {
                        db.Entry(scout).State = EntityState.Detached;
                    }

                    result.Errors.Add(new ScoutImportErrorDto
                    {
                        LineNumber = rowNumber,
                        Matricule = matricule,
                        Message = $"Enregistrement impossible: {ex.Message}"
                    });
                    result.SkippedCount++;
                }
                catch (DbUpdateException)
                {
                    if (isUpdate)
                    {
                        RestoreImportSnapshot(scout, snapshot!);
                        db.Entry(scout).State = EntityState.Unchanged;
                    }
                    else
                    {
                        db.Entry(scout).State = EntityState.Detached;
                    }

                    result.Errors.Add(new ScoutImportErrorDto
                    {
                        LineNumber = rowNumber,
                        Matricule = matricule,
                        Message = $"Enregistrement impossible: {ImportPersistenceErrorMessage}"
                    });
                    result.SkippedCount++;
                }
            }

            return result;
        }
    }

    public byte[] GenerateImportTemplate()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Scouts");

        var headers = new[]
        {
            "Matricule", "Nom", "Prenom", "DateNaissance", "LieuNaissance", "Sexe",
            "Telephone", "Email", "RegionScoute", "District", "NumeroCarte",
            "Fonction", "AdresseGeographique", "Groupe", "Branche"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#EAF4D7");
        }

        worksheet.Cell(2, 1).Value = string.Empty;
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
        worksheet.Cell(2, 13).Value = "Cocody Angre";
        worksheet.Cell(2, 14).Value = "Groupe Saint-Michel";
        worksheet.Cell(2, 15).Value = "Louveteaux";
        worksheet.Cell(2, 4).Style.DateFormat.Format = "dd/MM/yyyy";
        worksheet.Columns().AdjustToContents();
        worksheet.SheetView.FreezeRows(1);

        var guide = workbook.Worksheets.Add("Guide");
        guide.Cell("A1").Value = "Colonnes obligatoires";
        guide.Cell("A2").Value = "Nom, Prenom, DateNaissance";
        guide.Cell("A4").Value = "Regles";
        guide.Cell("A5").Value = $"La colonne Matricule est optionnelle et ne sert qu'a retrouver un scout deja cotise au format {ScoutMatriculeFormat.Example}.";
        guide.Cell("A6").Value = "Un nouveau scout peut etre importe sans matricule. Le matricule est attribue lors de la premiere cotisation nationale.";
        guide.Cell("A7").Value = "Groupe et Branche doivent correspondre aux noms deja presents dans l'application.";
        guide.Cell("A8").Value = "La colonne Fonction du fichier renseigne directement le champ Fonction du scout.";
        guide.Cell("A9").Value = "DateNaissance peut etre au format Excel date ou jj/MM/aaaa.";
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
        LieuNaissance = s.LieuNaissance,
        Sexe = s.Sexe,
        Telephone = s.Telephone,
        Email = s.Email,
        RegionScoute = s.RegionScoute,
        District = s.District,
        NumeroCarte = s.NumeroCarte,
        Fonction = s.Fonction,
        FonctionVieActive = s.FonctionVieActive,
        NiveauFormationScoute = s.NiveauFormationScoute,
        ContactUrgenceNom = s.ContactUrgenceNom,
        ContactUrgenceRelation = s.ContactUrgenceRelation,
        ContactUrgenceTelephone = s.ContactUrgenceTelephone,
        PhotoUrl = s.PhotoUrl,
        StatutASCCI = s.StatutASCCI,
        AssuranceAnnuelle = s.AssuranceAnnuelle,
        AdresseGeographique = s.AdresseGeographique,
        GroupeId = s.GroupeId,
        BrancheId = s.BrancheId,
        NomGroupe = s.Groupe?.Nom,
        NomBranche = s.Branche?.Nom,
        DerniereInscriptionAnnuelle = null,
        DerniereCotisationNationaleAjour = null,
        HistoriqueInscriptionsCount = 0
    };

    private async Task PopulateHistoryAsync(List<ScoutDto> scouts)
    {
        if (scouts.Count == 0)
        {
            return;
        }

        var scoutIds = scouts.Select(s => s.Id).Distinct().ToList();
        var inscriptions = await db.InscriptionsAnnuellesScouts.AsNoTracking()
            .Include(i => i.Groupe)
            .Include(i => i.Branche)
            .Where(i => scoutIds.Contains(i.ScoutId))
            .OrderByDescending(i => i.AnneeReference)
            .ThenByDescending(i => i.DateInscription)
            .Select(i => new
            {
                i.ScoutId,
                i.LibelleAnnee,
                i.CotisationNationaleAjour,
                GroupeNom = i.Groupe != null ? i.Groupe.Nom : null,
                BrancheNom = i.Branche != null ? i.Branche.Nom : null
            })
            .ToListAsync();

        var byScout = inscriptions.GroupBy(i => i.ScoutId).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var scout in scouts)
        {
            if (!byScout.TryGetValue(scout.Id, out var history) || history.Count == 0)
            {
                continue;
            }

            var latest = history[0];
            scout.DerniereInscriptionAnnuelle = string.IsNullOrWhiteSpace(latest.GroupeNom)
                ? latest.LibelleAnnee
                : $"{latest.LibelleAnnee} - {latest.GroupeNom}{(string.IsNullOrWhiteSpace(latest.BrancheNom) ? string.Empty : $" / {latest.BrancheNom}")}";
            scout.DerniereCotisationNationaleAjour = latest.CotisationNationaleAjour;
            scout.HistoriqueInscriptionsCount = history.Count;
        }
    }

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

    private static bool? ReadOptionalBoolean(IXLRow row, IDictionary<string, int> headerMap, params string[] headers)
    {
        var raw = ReadString(row, headerMap, headers);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return raw.Trim().ToLowerInvariant() switch
        {
            "oui" => true,
            "true" => true,
            "1" => true,
            "x" => true,
            "yes" => true,
            "non" => false,
            "false" => false,
            "0" => false,
            "no" => false,
            _ => false
        };
    }

    private static string ReadString(IXLRow row, IDictionary<string, int> headerMap, params string[] headers)
    {
        foreach (var header in headers)
        {
            if (headerMap.TryGetValue(header, out var column))
            {
                return row.Cell(column).GetString().Trim();
            }
        }

        return string.Empty;
    }

    private static string NormalizeHeader(string value)
    {
        return DatabaseText.NormalizeSearchKey(value);
    }

    private static string NormalizeLookup(string? value)
    {
        return DatabaseText.NormalizeSearchKey(value);
    }

    private static string BuildBranchLookupKey(Guid groupeId, string? brancheNom)
    {
        return $"{groupeId:N}:{NormalizeLookup(brancheNom)}";
    }

    private static string BuildScoutIdentityKey(string? nom, string? prenom, DateTime dateNaissance)
    {
        return $"{NormalizeLookup(nom)}|{NormalizeLookup(prenom)}|{dateNaissance:yyyyMMdd}";
    }

    private static string BuildScoutImportDisplayLabel(Scout scout)
    {
        var identity = $"{scout.Prenom} {scout.Nom}".Trim();
        return string.IsNullOrWhiteSpace(scout.Matricule)
            ? $"{identity} (sans matricule)"
            : $"{identity} ({scout.Matricule})";
    }
    private static Groupe? ResolveImportGroupe(Scout? existingScout, Groupe? providedGroupe)
    {
        return providedGroupe ?? (existingScout?.GroupeId is Guid existingGroupeId
            ? new Groupe { Id = existingGroupeId }
            : null);
    }

    private static Branche? ResolveImportBranche(
        string? brancheNom,
        Groupe? effectiveGroupe,
        Scout? existingScout,
        IReadOnlyDictionary<string, Branche> branchesByGroupAndName,
        IReadOnlyDictionary<Guid, Branche> branchesById,
        ICollection<string> rowErrors)
    {
        if (!string.IsNullOrWhiteSpace(brancheNom))
        {
            if (effectiveGroupe is null)
            {
                rowErrors.Add("Le groupe est obligatoire lorsqu'une branche est renseignee.");
                return null;
            }

            var branchKey = BuildBranchLookupKey(effectiveGroupe.Id, brancheNom);
            if (!branchesByGroupAndName.TryGetValue(branchKey, out var branche))
            {
                rowErrors.Add($"Branche introuvable pour le groupe renseigne: {brancheNom}.");
                return null;
            }

            return branche;
        }

        if (existingScout?.BrancheId is not Guid existingBrancheId)
        {
            return null;
        }

        if (!branchesById.TryGetValue(existingBrancheId, out var existingBranche))
        {
            return null;
        }

        if (effectiveGroupe is null || existingBranche.GroupeId == effectiveGroupe.Id)
        {
            return existingBranche;
        }

        rowErrors.Add("La branche existante du scout n'appartient pas au groupe renseigne. Precisez une branche compatible.");
        return null;
    }

    private static ScoutImportSnapshot CaptureImportSnapshot(Scout scout) => new(
        scout.Matricule,
        scout.Nom,
        scout.Prenom,
        scout.DateNaissance,
        scout.LieuNaissance,
        scout.Sexe,
        scout.Telephone,
        scout.Email,
        scout.RegionScoute,
        scout.District,
        scout.NumeroCarte,
        scout.Fonction,
        scout.AssuranceAnnuelle,
        scout.AdresseGeographique,
        scout.GroupeId,
        scout.BrancheId,
        scout.IsActive);

    private static void RestoreImportSnapshot(Scout scout, ScoutImportSnapshot snapshot)
    {
        scout.Matricule = snapshot.Matricule;
        scout.Nom = snapshot.Nom;
        scout.Prenom = snapshot.Prenom;
        scout.DateNaissance = snapshot.DateNaissance;
        scout.LieuNaissance = snapshot.LieuNaissance;
        scout.Sexe = snapshot.Sexe;
        scout.Telephone = snapshot.Telephone;
        scout.Email = snapshot.Email;
        scout.RegionScoute = snapshot.RegionScoute;
        scout.District = snapshot.District;
        scout.NumeroCarte = snapshot.NumeroCarte;
        scout.Fonction = snapshot.Fonction;
        scout.AssuranceAnnuelle = snapshot.AssuranceAnnuelle;
        scout.AdresseGeographique = snapshot.AdresseGeographique;
        scout.GroupeId = snapshot.GroupeId;
        scout.BrancheId = snapshot.BrancheId;
        scout.IsActive = snapshot.IsActive;
    }

    private static void ApplyImportValuesToScout(
        Scout scout,
        ScoutImportValues values,
        bool preserveExistingOptionalValues)
    {
        scout.Matricule = values.Matricule ?? (preserveExistingOptionalValues ? scout.Matricule : null);
        scout.Nom = values.Nom;
        scout.Prenom = values.Prenom;
        scout.DateNaissance = values.DateNaissance;
        scout.LieuNaissance = preserveExistingOptionalValues ? values.LieuNaissance ?? scout.LieuNaissance : values.LieuNaissance;
        scout.Sexe = preserveExistingOptionalValues ? values.Sexe ?? scout.Sexe : values.Sexe;
        scout.Telephone = preserveExistingOptionalValues ? values.Telephone ?? scout.Telephone : values.Telephone;
        scout.Email = preserveExistingOptionalValues ? values.Email ?? scout.Email : values.Email;
        scout.RegionScoute = preserveExistingOptionalValues ? values.RegionScoute ?? scout.RegionScoute : values.RegionScoute;
        scout.District = preserveExistingOptionalValues ? values.District ?? scout.District : values.District;
        scout.NumeroCarte = preserveExistingOptionalValues ? values.NumeroCarte ?? scout.NumeroCarte : values.NumeroCarte;
        scout.Fonction = preserveExistingOptionalValues ? values.Fonction ?? scout.Fonction : values.Fonction;
        scout.AssuranceAnnuelle = preserveExistingOptionalValues ? scout.AssuranceAnnuelle : false;
        scout.AdresseGeographique = preserveExistingOptionalValues ? values.AdresseGeographique ?? scout.AdresseGeographique : values.AdresseGeographique;
        scout.GroupeId = values.GroupeId ?? (preserveExistingOptionalValues ? scout.GroupeId : null);
        scout.BrancheId = values.BrancheId ?? (preserveExistingOptionalValues ? scout.BrancheId : null);
    }

    private async Task EnsureUniqueMatriculeAsync(string? matricule, Guid? currentId = null)
    {
        if (string.IsNullOrWhiteSpace(matricule))
        {
            return;
        }

        var exists = db.Database.IsNpgsql()
            ? await db.Scouts.AnyAsync(s =>
                s.Id != currentId &&
                s.Matricule != null &&
                s.Matricule == matricule)
            : await db.Scouts.AnyAsync(s =>
                s.Id != currentId &&
                s.Matricule != null &&
                s.Matricule.ToUpper() == DatabaseText.NormalizeCaseInsensitiveKey(matricule));

        if (exists)
        {
            throw new InvalidOperationException("Le matricule existe deja.");
        }
    }

    private static void ValidateManualMatriculeInput(string? requestedMatricule, string? existingMatricule = null)
    {
        if (string.IsNullOrWhiteSpace(requestedMatricule))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(existingMatricule)
            && string.Equals(ScoutMatriculeFormat.Normalize(existingMatricule), requestedMatricule, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException("Le matricule est attribue automatiquement lors de la premiere cotisation nationale.");
    }

    private async Task EnsureUniqueNumeroCarteAsync(string? numeroCarte, Guid? currentId = null)
    {
        if (string.IsNullOrWhiteSpace(numeroCarte))
        {
            return;
        }

        var exists = db.Database.IsNpgsql()
            ? await db.Scouts.AnyAsync(s =>
                s.Id != currentId &&
                s.NumeroCarte != null &&
                s.NumeroCarte == numeroCarte)
            : await db.Scouts.AnyAsync(s =>
                s.Id != currentId &&
                s.NumeroCarte != null &&
                s.NumeroCarte.ToUpper() == DatabaseText.NormalizeCaseInsensitiveKey(numeroCarte));

        if (exists)
        {
            throw new InvalidOperationException("Le numero de carte existe deja.");
        }
    }

    private async Task ValidateAffectationAsync(Guid? groupeId, Guid? brancheId)
    {
        if (groupeId.HasValue)
        {
            var groupeExists = await db.Groupes.AnyAsync(g => g.Id == groupeId.Value && g.IsActive);
            if (!groupeExists)
            {
                throw new InvalidOperationException("Le groupe selectionne est introuvable ou inactif.");
            }
        }

        if (!brancheId.HasValue)
        {
            return;
        }

        if (!groupeId.HasValue)
        {
            throw new InvalidOperationException("Le groupe est obligatoire lorsqu'une branche est selectionnee.");
        }

        var branche = await db.Branches
            .Where(b => b.Id == brancheId.Value && b.IsActive)
            .Select(b => new { b.GroupeId })
            .FirstOrDefaultAsync();

        if (branche is null)
        {
            throw new InvalidOperationException("La branche selectionnee est introuvable ou inactive.");
        }

        if (branche.GroupeId != groupeId.Value)
        {
            throw new InvalidOperationException("La branche selectionnee doit appartenir au groupe selectionne.");
        }
    }

    private static string NormalizeRequired(string value, string errorMessage)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException(errorMessage);
        }

        return normalized;
    }

    private static DateTime NormalizeDateNaissance(DateTime value)
    {
        if (value == default)
        {
            throw new InvalidOperationException("La date de naissance est requise.");
        }

        if (value.Date > DateTime.UtcNow.Date)
        {
            throw new InvalidOperationException("La date de naissance ne peut pas etre dans le futur.");
        }

        return value.Date;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private async Task SaveChangesAsync()
    {
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.ReferencesConstraint(PersistenceConstraints.ScoutsMatricule))
        {
            throw new InvalidOperationException("Le matricule existe deja.", ex);
        }
        catch (DbUpdateException ex) when (ex.ReferencesConstraint(PersistenceConstraints.ScoutsNumeroCarte))
        {
            throw new InvalidOperationException("Le numero de carte existe deja.", ex);
        }
    }

    private sealed record ScoutImportValues
    {
        public string? Matricule { get; init; }
        public string Nom { get; init; } = string.Empty;
        public string Prenom { get; init; } = string.Empty;
        public DateTime DateNaissance { get; init; }
        public string? LieuNaissance { get; init; }
        public string? Sexe { get; init; }
        public string? Telephone { get; init; }
        public string? Email { get; init; }
        public string? RegionScoute { get; init; }
        public string? District { get; init; }
        public string? NumeroCarte { get; init; }
        public string? Fonction { get; init; }
        public string? AdresseGeographique { get; init; }
        public Guid? GroupeId { get; init; }
        public Guid? BrancheId { get; init; }
    }

    private sealed record ScoutImportSnapshot(
        string? Matricule,
        string Nom,
        string Prenom,
        DateTime DateNaissance,
        string? LieuNaissance,
        string? Sexe,
        string? Telephone,
        string? Email,
        string? RegionScoute,
        string? District,
        string? NumeroCarte,
        string? Fonction,
        bool AssuranceAnnuelle,
        string? AdresseGeographique,
        Guid? GroupeId,
        Guid? BrancheId,
        bool IsActive);
}







