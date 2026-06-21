using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MangoTaika.Services;

public sealed class GlobalPermissionFilter(IPermissionService permissionService) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.Filters.Any(f => f is IAllowAnonymousFilter))
        {
            return;
        }

        if (context.ActionDescriptor is not ControllerActionDescriptor descriptor)
        {
            return;
        }

        var permissionCode = PermissionRouteMap.Resolve(descriptor.ControllerName, descriptor.ActionName);
        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            return;
        }

        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return;
        }

        if (!await permissionService.HasPermissionAsync(user, permissionCode))
        {
            context.Result = new ForbidResult();
        }
    }
}
