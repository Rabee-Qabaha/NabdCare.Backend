using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Common;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Api.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ITenantContext _tenant;
    private readonly NabdCareDbContext _db;

    public PermissionAuthorizationHandler(
        ITenantContext tenant,
        NabdCareDbContext db)
    {
        _tenant = tenant;
        _db = db;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var http = (context.Resource as DefaultHttpContext) ?? null; // depending on how you get services
        var evaluator = http?.RequestServices.GetRequiredService<IPermissionEvaluator>();
        if (evaluator == null) return;

        // requirement.Permission is currently "Permission.X". Normalize:
        var perm = requirement.Permission.Replace("Permission.", string.Empty);

        if (await evaluator.HasAsync(perm))
            context.Succeed(requirement);
    }
}