namespace NabdCare.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(string userId, string email, string roleName, Guid roleId, Guid? clinicId,
        string fullName);

    string GenerateRefreshToken();
}