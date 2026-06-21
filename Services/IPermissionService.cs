using System.Security.Claims;

namespace MangoTaika.Services;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(ClaimsPrincipal user, string permissionCode);
    Task<IReadOnlySet<string>> GetPermissionCodesAsync(ClaimsPrincipal user);
}
