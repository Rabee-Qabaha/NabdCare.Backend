using AutoMapper;
using NabdCare.Application.DTOs.Roles;
using NabdCare.Domain.Entities.Roles;

namespace NabdCare.Application.Mappings;

/// <summary>
/// AutoMapper profile for Role entity mapping
/// Handles conversions between Role domain entity and DTOs
/// 
/// Author: Rabee-Qabaha
/// Updated: 2025-11-09 21:01:40 UTC
/// </summary>
public class RoleProfile : Profile
{
    public RoleProfile()
    {
        // ============================================================
        // Role -> RoleResponseDto
        // ============================================================
        CreateMap<Role, RoleResponseDto>()
            // Map clinic name
            .ForMember(dest => dest.ClinicName, 
                opt => opt.MapFrom(src => src.Clinic != null ? src.Clinic.Name : null))
            
            .ForMember(dest => dest.CreatedByUserName, 
                opt => opt.MapFrom(src => ExtractUserDisplayName(src.CreatedBy)))
            .ForMember(dest => dest.UpdatedByUserName, 
                opt => opt.MapFrom(src => ExtractUserDisplayName(src.UpdatedBy)))
            .ForMember(dest => dest.DeletedByUserName, 
                opt => opt.MapFrom(src => ExtractUserDisplayName(src.DeletedBy)))
            
            // These are set manually in the service layer (calculated counts)
            .ForMember(dest => dest.UserCount, opt => opt.Ignore())
            .ForMember(dest => dest.PermissionCount, opt => opt.Ignore());

        // ============================================================
        // CreateRoleRequestDto -> Role
        // ============================================================
        CreateMap<CreateRoleRequestDto, Role>()
            // Don't map these - they're set by the system
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.IsSystemRole, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.IsTemplate, opt => opt.MapFrom(src => false))
            
            // Audit fields - set by DbContext
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            
            // Soft delete fields - set by DbContext
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            
            // Navigation properties - managed separately
            .ForMember(dest => dest.Clinic, opt => opt.Ignore())
            .ForMember(dest => dest.Users, opt => opt.Ignore())
            .ForMember(dest => dest.RolePermissions, opt => opt.Ignore())
            .ForMember(dest => dest.TemplateRole, opt => opt.Ignore());

        // ============================================================
        // UpdateRoleRequestDto -> Role
        // ============================================================
        CreateMap<UpdateRoleRequestDto, Role>()
            // Immutable fields - cannot be changed after creation
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.IsSystemRole, opt => opt.Ignore()) // ✅ Cannot change
            .ForMember(dest => dest.IsTemplate, opt => opt.Ignore()) // ✅ Cannot change
            .ForMember(dest => dest.ClinicId, opt => opt.Ignore()) // ✅ Cannot change
            .ForMember(dest => dest.TemplateRoleId, opt => opt.Ignore()) // ✅ Cannot change
            
            // Audit fields - set by DbContext
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            
            // Soft delete fields - set by DbContext
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            
            // Navigation properties - managed separately
            .ForMember(dest => dest.Clinic, opt => opt.Ignore())
            .ForMember(dest => dest.Users, opt => opt.Ignore())
            .ForMember(dest => dest.RolePermissions, opt => opt.Ignore())
            .ForMember(dest => dest.TemplateRole, opt => opt.Ignore());
    }

    /// <summary>
    /// Extract user display name from audit field format: "UserId|UserFullName"
    /// 
    /// Examples:
    /// - Input: "550e8400-e29b-41d4-a716-446655440000|Rabee Qabaha"
    ///   Output: "Rabee Qabaha"
    /// 
    /// - Input: "550e8400-e29b-41d4-a716-446655440000|"
    ///   Output: "550e8400-e29b-41d4-a716-446655440000"
    /// 
    /// - Input: null or empty
    ///   Output: null
    /// 
    /// Fallback behavior:
    /// If the format doesn't match expected pattern, returns the entire input string.
    /// This ensures audit data is always displayed, even if format is unexpected.
    /// </summary>
    /// <param name="userIdentifier">Combined user identifier string (UserId|UserFullName)</param>
    /// <returns>Extracted display name or full identifier if parsing fails</returns>
    private static string? ExtractUserDisplayName(string? userIdentifier)
    {
        // Handle null or empty input
        if (string.IsNullOrWhiteSpace(userIdentifier))
            return null;

        // Split by pipe separator
        var parts = userIdentifier.Split('|');
        
        // If format is "UserId|UserFullName" and display name is not empty
        if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[1]))
            return parts[1].Trim();

        // Fallback: return the entire string (for backward compatibility)
        // This handles cases where only UserId was stored
        return userIdentifier.Trim();
    }
}