namespace NabdCare.Application.Common;

public interface IUserContext
{
    // Method to retrieve the current user's ID
    string GetCurrentUserId();
    string? GetCurrentUserRoleId();
}