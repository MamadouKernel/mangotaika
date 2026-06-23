using MangoTaika.Data;
using MangoTaika.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MangoTaika.Services;

/// <summary>
/// Libere les identifiants de connexion d'un compte retire de la liste (soft delete) afin que
/// la personne puisse a nouveau s'inscrire avec le meme numero de telephone / matricule.
/// Sans cela, le compte supprime conserve son UserName (contrainte d'unicite Identity), son
/// numero de telephone et ses liens scout/parent, ce qui bloque toute nouvelle inscription.
/// </summary>
public static class UserAccountCleanup
{
    public const string DeletedUserNamePrefix = "deleted-";

    /// <summary>
    /// Indique si les identifiants du compte ont deja ete liberes.
    /// </summary>
    public static bool HasReleasedIdentifiers(ApplicationUser user)
        => user.UserName is not null
           && user.UserName.StartsWith(DeletedUserNamePrefix, StringComparison.Ordinal);

    /// <summary>
    /// Detache les identifiants uniques et les liens scout/parent du compte. Les mutations sont
    /// appliquees sur les entites suivies ; l'appelant reste responsable du SaveChanges.
    /// </summary>
    public static async Task ReleaseIdentifiersAsync(AppDbContext db, ApplicationUser user)
    {
        user.UserName = $"{DeletedUserNamePrefix}{user.Id:N}";
        user.NormalizedUserName = user.UserName.ToUpperInvariant();
        user.PhoneNumber = null;
        user.PhoneNumberConfirmed = false;

        var scouts = await db.Scouts.Where(s => s.UserId == user.Id).ToListAsync();
        foreach (var scout in scouts)
        {
            scout.UserId = null;
        }

        var parents = await db.Parents.Where(p => p.UserId == user.Id).ToListAsync();
        foreach (var parent in parents)
        {
            parent.UserId = null;
        }
    }
}
