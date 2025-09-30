using NabdCare.Domain.Enums;

namespace NabdCare.Application.DTOs.Users;

public class CreateUserRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Guid? ClinicId { get; set; }
}