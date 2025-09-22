namespace NabdCare.Application.Interfaces.Auth;

public interface IAuthService
{
    Task<(string accessToken, string refreshToken)> LoginAsync(string email, string password);
    Task<(string accessToken, string refreshToken)> RefreshTokenAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
}