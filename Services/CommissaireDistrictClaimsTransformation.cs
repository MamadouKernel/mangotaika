using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace MangoTaika.Services;

public sealed class CommissaireDistrictClaimsTransformation : IClaimsTransformation
{
    public const string AliasClaimType = "MangoTaika.RoleAlias";

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (!principal.IsInRole(RoleNames.CommissaireDistrict) || principal.IsInRole(RoleNames.Administrateur))
        {
            return Task.FromResult(principal);
        }

        if (principal.Identity is ClaimsIdentity identity && identity.IsAuthenticated)
        {
            identity.AddClaim(new Claim(identity.RoleClaimType, RoleNames.Administrateur));
            identity.AddClaim(new Claim(AliasClaimType, RoleNames.Administrateur));
        }

        return Task.FromResult(principal);
    }

    public static bool HasAdministrateurAlias(ClaimsPrincipal user)
        => user.HasClaim(AliasClaimType, RoleNames.Administrateur);
}
