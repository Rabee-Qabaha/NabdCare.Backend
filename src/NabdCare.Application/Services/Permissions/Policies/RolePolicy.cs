using NabdCare.Application.Common;
using NabdCare.Application.DTOs.Roles;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Domain.Entities.Roles;

namespace NabdCare.Application.Services.Permissions.Policies;

/// <summary>
/// ABAC policy for roles.
/// Handles authorization for both Role Entities and Role DTOs.
/// </summary>
public class RolePolicy : IAccessPolicy<Role>, IAccessPolicy<RoleResponseDto>
{
    // 1. Evaluate Domain Entity (Used inside Services if needed)
    public Task<bool> EvaluateAsync(ITenantContext user, string action, Role role)
    {
        return Task.FromResult(CheckAccess(user, role.IsSystemRole, role.ClinicId));
    }

    // 2. Evaluate DTO (Used in API Endpoints for Clean Architecture)
    public Task<bool> EvaluateAsync(ITenantContext user, string action, RoleResponseDto role)
    {
        // Assuming RoleResponseDto has Guid? ClinicId. 
        // If it's a string in your C# DTO, perform Guid.Parse(role.ClinicId)
        return Task.FromResult(CheckAccess(user, role.IsSystemRole, role.ClinicId));
    }

    // 3. Shared Logic (Single Source of Truth)
    private bool CheckAccess(ITenantContext user, bool isSystemRole, Guid? resourceClinicId)
    {
        // Rule 1: SuperAdmin can access everything
        if (user.IsSuperAdmin) 
            return true;

        // Rule 2: Non-SuperAdmins cannot touch System Roles
        // (This prevents a ClinicAdmin from editing/deleting a System Role even if they could see it)
        if (isSystemRole) 
            return false;

        // Rule 3: Clinic Match
        // Allow if the user belongs to a clinic AND the role belongs to the same clinic
        if (user.ClinicId.HasValue && resourceClinicId == user.ClinicId)
            return true;

        return false;
    }
}