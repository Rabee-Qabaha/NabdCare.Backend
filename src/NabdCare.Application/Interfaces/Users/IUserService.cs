using NabdCare.Application.DTOs.Users;
using NabdCare.Domain.Enums;

namespace NabdCare.Application.Interfaces.Users;

public interface IUserService
{
    Task<UserResponseDto> CreateUserAsync(CreateUserRequestDto dto);
    Task<UserResponseDto?> GetUserByIdAsync(Guid id);
    Task<IEnumerable<UserResponseDto>> GetUsersByClinicIdAsync(Guid? clinicId);
    Task<UserResponseDto?> UpdateUserAsync(Guid id, UpdateUserRequestDto dto);
    Task<UserResponseDto?> UpdateUserRoleAsync(Guid id, UserRole newRole);
    Task<bool> SoftDeleteUserAsync(Guid id);
    Task<bool> DeleteUserAsync(Guid id);
}