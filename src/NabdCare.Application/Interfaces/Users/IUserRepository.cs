using NabdCare.Domain.Entities.Users;

namespace NabdCare.Application.Interfaces.Users;

public interface IUserRepository
{
    Task<User?> GetUserByIdAsync(Guid userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<IEnumerable<User>> GetUsersByClinicIdAsync(Guid? clinicId);
    Task<User> CreateUserAsync(User user);
    Task<User> UpdateUserAsync(User user);
    Task<bool> DeleteUserAsync(Guid userId);
    Task<bool> SoftDeleteUserAsync(Guid userId);
}