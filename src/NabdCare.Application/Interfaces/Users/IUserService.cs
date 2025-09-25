using NabdCare.Domain.Entities.User;

namespace NabdCare.Application.Interfaces.Users;

public interface IUserService
{
    Task<User> CreateUserAsync(User user);
    Task<User?> GetUserByIdAsync(Guid userId);
    Task<IEnumerable<User>> GetUsersByClinicIdAsync(Guid? clinicId);
    Task<User> UpdateUserAsync(User user);
    Task<bool> DeleteUserAsync(Guid userId);
    Task<bool> SoftDeleteUserAsync(Guid userId);
}