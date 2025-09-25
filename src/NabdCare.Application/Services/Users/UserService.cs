using NabdCare.Application.Interfaces.Users;
using NabdCare.Domain.Entities.User;

namespace NabdCare.Application.Services.Users;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User> CreateUserAsync(User user)
    {
        try
        {
            return await _userRepository.CreateUserAsync(user);
        }
        catch (Exception ex)
        {
            // Log the error (if logging is configured)
            throw new InvalidOperationException("An error occurred while creating the user.", ex);
        }
    }

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        try
        {
            return await _userRepository.GetUserByIdAsync(userId)
                   ?? throw new KeyNotFoundException($"User with ID {userId} not found.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An error occurred while fetching the user.", ex);
        }
    }

    public async Task<IEnumerable<User>> GetUsersByClinicIdAsync(Guid? clinicId)
    {
        try
        {
            return await _userRepository.GetUsersByClinicIdAsync(clinicId);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An error occurred while fetching users.", ex);
        }
    }

    public async Task<User> UpdateUserAsync(User user)
    {
        try
        {
            if (user.Id == Guid.Empty)
                throw new ArgumentException("User ID is required for updating a user.");

            return await _userRepository.UpdateUserAsync(user);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An error occurred while updating the user.", ex);
        }
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        try
        {
            var success = await _userRepository.DeleteUserAsync(userId);
            if (!success)
                throw new KeyNotFoundException($"User with ID {userId} not found for deletion.");

            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An error occurred while deleting the user.", ex);
        }
    }

    public async Task<bool> SoftDeleteUserAsync(Guid userId)
    {
        try
        {
            return await _userRepository.SoftDeleteUserAsync(userId);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An error occurred while deleting the user.", ex);
        }
    }
}