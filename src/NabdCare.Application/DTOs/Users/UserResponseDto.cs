using NabdCare.Domain.Enums;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Users;

[ExportTsClass]
public class UserResponseDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public Guid? ClinicId { get; set; }
    public string? ClinicName { get; set; }
}