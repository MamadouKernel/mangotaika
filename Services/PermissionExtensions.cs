using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace MangoTaika.Services;

public static class PermissionExtensions
{
    public static async Task<bool> HasPermissionAsync(this ClaimsPrincipal user, IPermissionService service, string permissionCode)
    {
        return await service.HasPermissionAsync(user, permissionCode);
    }

    public static async Task<bool> HasPermissionAsync(this HttpContext context, string permissionCode)
    {
        if (context.RequestServices.GetService(typeof(IPermissionService)) is not IPermissionService service)
        {
            return false;
        }

        return await service.HasPermissionAsync(context.User, permissionCode);
    }
}
