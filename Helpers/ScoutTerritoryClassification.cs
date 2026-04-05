using MangoTaika.Data.Entities;

namespace MangoTaika.Helpers;

public static class ScoutTerritoryClassification
{
    public static bool IsJeuneScout(Scout scout, string? brancheNom = null)
    {
        if (IsYouthBranchName(brancheNom ?? scout.Branche?.Nom))
        {
            return true;
        }

        var today = DateTime.UtcNow.Date;
        var birthDate = scout.DateNaissance.Date;
        var age = today.Year - birthDate.Year;

        if (birthDate > today.AddYears(-age))
        {
            age--;
        }

        return age < 18;
    }

    public static bool IsYouthBranchName(string? brancheNom)
    {
        var normalized = DatabaseText.NormalizeSearchKey(brancheNom ?? string.Empty);
        return normalized.Contains("CHEMINOT")
            || normalized.Contains("ROUTE")
            || normalized.Contains("ROUTIER");
    }
}
