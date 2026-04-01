using MangoTaika.Data;
using MangoTaika.Data.Entities;
using MangoTaika.DTOs;
using MangoTaika.Helpers;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Services;

public class GroupeService(AppDbContext db, IGeocodingService geocoding, DistrictBranchInheritanceService districtBranchInheritance) : IGroupeService
{
    public async Task<List<GroupeDto>> GetAllAsync()
    {
        return await db.Groupes
            .Include(g => g.Responsable)
            .Where(g => g.IsActive)
            .Select(g => new GroupeDto
            {
                Id = g.Id,
                Nom = g.Nom,
                Description = g.Description,
                LogoUrl = g.LogoUrl,
                Latitude = g.Latitude,
                Longitude = g.Longitude,
                Adresse = g.Adresse,
                NomChefGroupe = g.NomChefGroupe != null && g.NomChefGroupe != string.Empty
                    ? g.NomChefGroupe
                    : (g.Responsable != null ? g.Responsable.Prenom + " " + g.Responsable.Nom : null),
                ResponsableId = g.ResponsableId,
                ContactChefGroupe = NormalizeOptional(g.Responsable != null ? g.Responsable.PhoneNumber : null),
                ResponsablePhotoUrl = NormalizeOptional(g.Responsable != null ? g.Responsable.PhotoUrl : null),
                NombreMembres = db.Scouts.Count(s => s.GroupeId == g.Id && s.IsActive),
                BranchesScouts = db.Branches
                    .Where(b => b.GroupeId == g.Id && b.IsActive)
                    .Select(b => new BrancheScoutCountDto
                    {
                        Nom = b.Nom,
                        NombreScouts = db.Scouts.Count(s => s.BrancheId == b.Id && s.IsActive),
                        NombreFilles = db.Scouts.Count(s => s.BrancheId == b.Id && s.IsActive && (
                            s.Sexe == "F" || s.Sexe == "Feminin" || s.Sexe == "Fille")),
                        NombreGarcons = db.Scouts.Count(s => s.BrancheId == b.Id && s.IsActive && (
                            s.Sexe == "M" || s.Sexe == "Masculin" || s.Sexe == "Garcon")),
                        NomChefUnite = b.NomChefUnite
                    }).ToList()
            })
            .ToListAsync();
    }

    public async Task<GroupeDto?> GetByIdAsync(Guid id)
    {
        var groupe = await db.Groupes
            .Include(g => g.Responsable)
            .FirstOrDefaultAsync(g => g.Id == id);
        if (groupe is null) return null;

        var scouts = await db.Scouts
            .Include(s => s.Branche)
            .Where(s => s.GroupeId == groupe.Id && s.IsActive)
            .ToListAsync();

        var branches = (await db.Branches
                .Where(b => b.GroupeId == groupe.Id && b.IsActive)
                .ToListAsync())
            .OrderBy(b => BranchOrdering.GetSortWeight(b.Nom))
            .ThenBy(b => b.AgeMin ?? int.MaxValue)
            .ThenBy(b => b.Nom)
            .ToList();

        var chefGroupeScout = FindChefGroupeScout(groupe, scouts);
        var branchNamesById = branches.ToDictionary(b => b.Id, b => b.Nom);

        return new GroupeDto
        {
            Id = groupe.Id,
            Nom = groupe.Nom,
            Description = groupe.Description,
            LogoUrl = groupe.LogoUrl,
            Latitude = groupe.Latitude,
            Longitude = groupe.Longitude,
            Adresse = groupe.Adresse,
            NomChefGroupe = chefGroupeScout != null
                ? BuildScoutDisplayName(chefGroupeScout)
                : BuildChefGroupeName(
                    groupe.NomChefGroupe,
                    groupe.Responsable != null ? $"{groupe.Responsable.Prenom} {groupe.Responsable.Nom}" : null),
            ChefGroupeScoutId = chefGroupeScout?.Id,
            ResponsableId = chefGroupeScout?.UserId ?? groupe.ResponsableId,
            ContactChefGroupe = NormalizeOptional(chefGroupeScout?.Telephone) ?? NormalizeOptional(groupe.Responsable?.PhoneNumber),
            ResponsablePhotoUrl = NormalizeOptional(chefGroupeScout?.PhotoUrl) ?? NormalizeOptional(groupe.Responsable?.PhotoUrl),
            NombreMembres = scouts.Count,
            NombreFilles = scouts.Count(s => ClassifySexe(s.Sexe) == SexeCategory.Feminin),
            NombreGarcons = scouts.Count(s => ClassifySexe(s.Sexe) == SexeCategory.Masculin),
            Jeunes = BuildRepartition(scouts.Where(IsJeune)),
            Adultes = BuildRepartition(scouts.Where(s => !IsJeune(s))),
            BranchesScouts = branches.Select(b =>
            {
                var branchScouts = scouts.Where(s => s.BrancheId == b.Id).ToList();
                return new BrancheScoutCountDto
                {
                    Nom = b.Nom,
                    NombreScouts = branchScouts.Count,
                    NombreFilles = branchScouts.Count(s => ClassifySexe(s.Sexe) == SexeCategory.Feminin),
                    NombreGarcons = branchScouts.Count(s => ClassifySexe(s.Sexe) == SexeCategory.Masculin),
                    NomChefUnite = b.NomChefUnite,
                    Jeunes = BuildRepartition(branchScouts.Where(IsJeune)),
                    Adultes = BuildRepartition(branchScouts.Where(s => !IsJeune(s)))
                };
            }).ToList(),
            Membres = scouts
                .Select(s => new
                {
                    Scout = s,
                    Branche = s.Branche?.Nom ?? (s.BrancheId.HasValue && branchNamesById.TryGetValue(s.BrancheId.Value, out var brancheNom)
                        ? brancheNom
                        : null)
                })
                .OrderBy(x => IsJeune(x.Scout) ? 0 : 1)
                .ThenBy(x => BranchOrdering.GetSortWeight(x.Branche))
                .ThenBy(x => x.Branche)
                .ThenBy(x => x.Scout.Nom)
                .ThenBy(x => x.Scout.Prenom)
                .Select(x => new GroupeMembreDto
                {
                    Matricule = x.Scout.Matricule,
                    Nom = x.Scout.Nom,
                    Prenoms = x.Scout.Prenom,
                    Branche = x.Branche,
                    Fonction = NormalizeOptional(x.Scout.Fonction) ?? "-",
                    TypeMembre = IsJeune(x.Scout) ? "Jeune" : "Adulte"
                })
                .ToList()
        };
    }

    public async Task<GroupeDto> CreateAsync(GroupeCreateDto dto)
    {
        var nom = NormalizeNom(dto.Nom);
        ValidateCoordinates(dto.Latitude, dto.Longitude);
        await EnsureUniqueNomAsync(nom);
        var adresse = BuildAdresse(dto.Commune, dto.Quartier);
        var (lat, lng) = await geocoding.GeocodeAsync(adresse ?? "");

        var groupe = new Groupe
        {
            Id = Guid.NewGuid(),
            Nom = nom,
            Description = NormalizeOptional(dto.Description),
            LogoUrl = NormalizeOptional(dto.LogoUrl),
            Latitude = lat ?? dto.Latitude,
            Longitude = lng ?? dto.Longitude,
            Adresse = adresse,
            NomChefGroupe = null,
            ResponsableId = null
        };
        db.Groupes.Add(groupe);
        await SaveChangesAsync();
        await districtBranchInheritance.InheritDistrictBranchesForGroupAsync(groupe);
        return ToDto(groupe);
    }

    public async Task<bool> UpdateAsync(Guid id, GroupeCreateDto dto)
    {
        var groupe = await db.Groupes.FindAsync(id);
        if (groupe is null) return false;

        var nom = NormalizeNom(dto.Nom);
        ValidateCoordinates(dto.Latitude, dto.Longitude);
        await EnsureUniqueNomAsync(nom, id);
        var adresse = BuildAdresse(dto.Commune, dto.Quartier);
        var (lat, lng) = await geocoding.GeocodeAsync(adresse ?? "");
        var chefGroupeScout = await GetChefGroupeScoutAsync(groupe, dto.ChefGroupeScoutId);

        groupe.Nom = nom;
        groupe.Description = NormalizeOptional(dto.Description);
        groupe.LogoUrl = NormalizeOptional(dto.LogoUrl);
        groupe.Latitude = lat ?? dto.Latitude;
        groupe.Longitude = lng ?? dto.Longitude;
        groupe.Adresse = adresse;
        groupe.NomChefGroupe = chefGroupeScout != null ? BuildScoutDisplayName(chefGroupeScout) : null;
        groupe.ResponsableId = chefGroupeScout?.UserId;
        await SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var groupe = await db.Groupes.FindAsync(id);
        if (groupe is null) return false;
        groupe.IsActive = false;
        await db.SaveChangesAsync();
        return true;
    }

    private static GroupeDto ToDto(Groupe g) => new()
    {
        Id = g.Id,
        Nom = g.Nom,
        Description = g.Description,
        LogoUrl = g.LogoUrl,
        Latitude = g.Latitude,
        Longitude = g.Longitude,
        Adresse = g.Adresse,
        NomChefGroupe = BuildChefGroupeName(
            g.NomChefGroupe,
            g.Responsable != null ? $"{g.Responsable.Prenom} {g.Responsable.Nom}" : null),
        ResponsableId = g.ResponsableId,
        ContactChefGroupe = NormalizeOptional(g.Responsable?.PhoneNumber),
        ResponsablePhotoUrl = NormalizeOptional(g.Responsable?.PhotoUrl),
        NombreMembres = 0
    };

    private static string? BuildAdresse(string? commune, string? quartier)
    {
        var parts = new[] { NormalizeOptional(quartier), NormalizeOptional(commune) }
            .Where(p => p is not null);
        var result = string.Join(", ", parts);
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    private static string? BuildChefGroupeName(string? storedName, string? fallbackName)
    {
        return NormalizeOptional(storedName) ?? NormalizeOptional(fallbackName);
    }

    private static string BuildScoutDisplayName(Scout scout)
    {
        return string.Join(" ", new[] { scout.Prenom, scout.Nom }.Where(value => !string.IsNullOrWhiteSpace(value))).Trim();
    }

    private static bool IsDistrictEquipe(Groupe groupe)
    {
        return DatabaseText.NormalizeSearchKey(groupe.Nom ?? string.Empty)
            == DatabaseText.NormalizeSearchKey("Equipe de District Mango Taika");
    }

    private static bool HasChefGroupeFunction(string? fonction)
    {
        return DatabaseText.NormalizeSearchKey(fonction ?? string.Empty) == DatabaseText.NormalizeSearchKey("CHEF DE GROUPE (CG)");
    }

    private static bool HasDistrictEquipeFunction(string? fonction)
    {
        var normalizedFunction = DatabaseText.NormalizeSearchKey(fonction ?? string.Empty);
        return normalizedFunction == DatabaseText.NormalizeSearchKey("COMMISSAIRE DE DISTRICT (CD)");
    }

    private static bool IsChefGroupeScout(Scout scout, Groupe groupe)
    {
        if (scout.GroupeId != groupe.Id)
        {
            return false;
        }

        if (IsDistrictEquipe(groupe))
        {
            return HasDistrictEquipeFunction(scout.Fonction);
        }

        return scout.BrancheId.HasValue
            && scout.Branche != null
            && scout.Branche.GroupeId == groupe.Id
            && HasChefGroupeFunction(scout.Fonction);
    }

    private static string GetChefGroupeSelectionMessage(Groupe groupe)
    {
        return IsDistrictEquipe(groupe)
            ? "Pour l'Equipe de District Mango Taika, le chef de groupe selectionne doit etre un scout actif de cette entite avec la fonction COMMISSAIRE DE DISTRICT (CD)."
            : "Le chef de groupe selectionne doit etre un scout actif de cette entite avec la fonction CHEF DE GROUPE (CG).";
    }

    private static bool MatchesChefGroupeName(string? storedName, Scout scout)
    {
        var normalizedStoredName = DatabaseText.NormalizeSearchKey(storedName ?? string.Empty);
        if (string.IsNullOrEmpty(normalizedStoredName))
        {
            return false;
        }

        var prenomNom = DatabaseText.NormalizeSearchKey(BuildScoutDisplayName(scout));
        var nomPrenom = DatabaseText.NormalizeSearchKey($"{scout.Nom} {scout.Prenom}");
        return normalizedStoredName == prenomNom || normalizedStoredName == nomPrenom;
    }

    private static Scout? FindChefGroupeScout(Groupe groupe, IEnumerable<Scout> scouts)
    {
        var eligibleScouts = scouts
            .Where(s => IsChefGroupeScout(s, groupe))
            .ToList();

        if (groupe.ResponsableId.HasValue)
        {
            var byUser = eligibleScouts.FirstOrDefault(s => s.UserId == groupe.ResponsableId.Value);
            if (byUser is not null)
            {
                return byUser;
            }
        }

        return eligibleScouts.FirstOrDefault(s => MatchesChefGroupeName(groupe.NomChefGroupe, s));
    }

    private async Task<Scout?> GetChefGroupeScoutAsync(Groupe groupe, Guid? chefGroupeScoutId)
    {
        if (!chefGroupeScoutId.HasValue)
        {
            return null;
        }

        var scout = await db.Scouts
            .Include(s => s.Branche)
            .FirstOrDefaultAsync(s => s.Id == chefGroupeScoutId.Value && s.IsActive);

        if (scout is null)
        {
            throw new InvalidOperationException("Le chef de groupe selectionne est introuvable ou inactif.");
        }

        if (!IsChefGroupeScout(scout, groupe))
        {
            throw new InvalidOperationException(GetChefGroupeSelectionMessage(groupe));
        }

        return scout;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static RepartitionMembresDto BuildRepartition(IEnumerable<Scout> scouts)
    {
        var repartition = new RepartitionMembresDto();

        foreach (var scout in scouts)
        {
            switch (ClassifySexe(scout.Sexe))
            {
                case SexeCategory.Feminin:
                    repartition.NombreFeminin++;
                    break;
                case SexeCategory.Masculin:
                    repartition.NombreMasculin++;
                    break;
                default:
                    repartition.NombreNonRenseigne++;
                    break;
            }
        }

        return repartition;
    }

    private static string NormalizeNom(string nom)
    {
        var normalized = nom?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Le nom du groupe est obligatoire.");
        }

        return normalized;
    }

    private static void ValidateCoordinates(double? latitude, double? longitude)
    {
        if (latitude.HasValue && (latitude.Value < -90 || latitude.Value > 90))
        {
            throw new InvalidOperationException("La latitude doit etre comprise entre -90 et 90.");
        }

        if (longitude.HasValue && (longitude.Value < -180 || longitude.Value > 180))
        {
            throw new InvalidOperationException("La longitude doit etre comprise entre -180 et 180.");
        }
    }

    private async Task EnsureUniqueNomAsync(string nom, Guid? currentId = null)
    {
        var normalizedNom = DatabaseText.NormalizeSearchKey(nom);
        var exists = db.Database.IsNpgsql()
            ? await db.Groupes.AnyAsync(g =>
                g.IsActive &&
                g.Id != currentId &&
                g.NomNormalise == normalizedNom)
            : (await db.Groupes
                .Where(g => g.IsActive && g.Id != currentId)
                .Select(g => g.Nom)
                .ToListAsync())
                .Any(existingNom => DatabaseText.NormalizeSearchKey(existingNom) == normalizedNom);

        if (exists)
        {
            throw new InvalidOperationException("Un groupe avec ce nom existe deja.");
        }
    }

    private async Task SaveChangesAsync()
    {
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.ReferencesConstraint(PersistenceConstraints.GroupesNomNormaliseActif))
        {
            throw new InvalidOperationException("Un groupe avec ce nom existe deja.", ex);
        }
    }

    private static bool IsJeune(Scout scout)
    {
        var today = DateTime.UtcNow.Date;
        var birthDate = scout.DateNaissance.Date;
        var age = today.Year - birthDate.Year;

        if (birthDate > today.AddYears(-age))
        {
            age--;
        }

        return age < 18;
    }

    private static SexeCategory ClassifySexe(string? sexe)
    {
        var normalized = DatabaseText.NormalizeSearchKey(sexe ?? string.Empty);

        return normalized switch
        {
            "F" or "FEMININ" or "FILLE" => SexeCategory.Feminin,
            "M" or "MASCULIN" or "GARCON" => SexeCategory.Masculin,
            _ => SexeCategory.NonRenseigne
        };
    }

    private enum SexeCategory
    {
        NonRenseigne,
        Feminin,
        Masculin
    }
}




