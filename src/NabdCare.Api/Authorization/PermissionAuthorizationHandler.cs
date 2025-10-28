using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Common;
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

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // ✅ SuperAdmin bypass
        if (_tenant.IsSuperAdmin)
        {
            context.Succeed(requirement);
            return;
        }

        // ✅ Must be authenticated
        if (!_tenant.IsAuthenticated)
            return;

        // ✅ Must have RoleId in JWT
        if (!_tenant.RoleId.HasValue)
            return;

        // ✅ Check permission of role in DB
        var hasPermission = await _db.RolePermissions
            .Where(rp => rp.RoleId == _tenant.RoleId.Value)
            .Include(rp => rp.AppPermission)
            .AnyAsync(rp =>
                rp.AppPermission.Name == requirement.Permission &&
                !rp.IsDeleted);

        if (hasPermission)
            context.Succeed(requirement);
    }
}