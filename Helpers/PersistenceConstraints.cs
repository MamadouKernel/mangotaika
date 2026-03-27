namespace MangoTaika.Helpers;

public static class PersistenceConstraints
{
    public const string GroupesNomNormaliseActif = "IX_Groupes_NomNormalise_Actif";
    public const string BranchesGroupeNomNormaliseActif = "IX_Branches_GroupeId_NomNormalise_Actif";
    public const string ScoutsMatricule = "IX_Scouts_Matricule";
    public const string ScoutsNumeroCarte = "IX_Scouts_NumeroCarte";
}
