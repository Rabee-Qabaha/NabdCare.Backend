using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace NabdCare.Api.Authorization;

public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        : base(options) { }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        return policyName.StartsWith("Permission.", StringComparison.OrdinalIgnoreCase)
            ? new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(policyName))
                .Build()
            : await base.GetPolicyAsync(policyName);
    }
}