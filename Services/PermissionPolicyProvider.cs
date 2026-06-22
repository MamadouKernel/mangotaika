using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace MangoTaika.Services;

public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : IAuthorizationPolicyProvider
{
    public const string Prefix = "Perm:";

    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (string.IsNullOrWhiteSpace(policyName))
        {
            return _fallback.GetPolicyAsync(policyName);
        }

        var permissionCode = policyName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
            ? policyName[Prefix.Length..]
            : LooksLikePermissionCode(policyName) ? policyName : null;

        if (permissionCode is null)
        {
            return _fallback.GetPolicyAsync(policyName);
        }

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permissionCode))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }

    private static bool LooksLikePermissionCode(string value)
    {
        return value.Contains('.') && !value.Contains(' ');
    }
}
