using NabdCare.Domain.Entities.User;

namespace NabdCare.Application.Interfaces.Users;

public interface IUserRepository
{
    Task<User?> GetUserByIdAsync(Guid userId);
    Task<IEnumerable<User>> GetUsersByClinicIdAsync(Guid? clinicId);
    Task<User> CreateUserAsync(User user);
    Task<User> UpdateUserAsync(User user);
    Task<bool> DeleteUserAsync(Guid userId);
    Task<bool> SoftDeleteUserAsync(Guid userId);
}