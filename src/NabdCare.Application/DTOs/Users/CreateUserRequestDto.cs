using NabdCare.Domain.Enums;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Users;

[ExportTsClass]
public class CreateUserRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Guid? ClinicId { get; set; }
}