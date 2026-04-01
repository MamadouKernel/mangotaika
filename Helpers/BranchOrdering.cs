namespace MangoTaika.Helpers;

public static class BranchOrdering
{
    public static int GetSortWeight(string? nom)
    {
        var normalized = DatabaseText.NormalizeSearchKey(nom ?? string.Empty);

        return normalized switch
        {
            var value when value.Contains("OISILLON") || value.Contains("COLONIE") => 0,
            var value when value.Contains("LOUVETEAU") || value.Contains("MEUTE") => 1,
            var value when value.Contains("ECLAIREUR") || value.Contains("TROUPE") => 2,
            var value when value.Contains("CHEMINOT") || value.Contains("GENERATION") => 3,
            var value when value.Contains("ROUTE") || value.Contains("ROUTIER") || value.Contains("COMMUNAUTE") => 4,
            var value when value.Contains("BENEVOLE") || value.Contains("ADS") => 5,
            _ => 99
        };
    }
}
