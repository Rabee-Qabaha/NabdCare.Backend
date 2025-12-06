using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Users;

[ExportTsClass]
public class UserResponseDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? RoleColorCode { get; set; } 
    public string? RoleIcon { get; set; }
    public bool IsSystemRole { get; set; }
    
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    
    public Guid? ClinicId { get; set; }
    public string? ClinicName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    public Guid? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
}