using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MangoTaika.Services;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : TypeFilterAttribute
{
    public RequirePermissionAttribute(string permissionCode)
        : base(typeof(RequirePermissionFilter))
    {
        PermissionCode = permissionCode;
        Arguments = [permissionCode];
    }

    public string PermissionCode { get; }
}

public sealed class RequirePermissionFilter(
    string permissionCode,
    IPermissionService permissionService) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
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
