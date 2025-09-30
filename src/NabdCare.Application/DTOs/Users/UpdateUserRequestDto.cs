using NabdCare.Domain.Enums;

namespace NabdCare.Application.DTOs.Users;

public class UpdateUserRequestDto
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public UserRole Role { get; set; }
}