using System.Net.Http.Json;
using NabdCare.Application.DTOs.Auth;

namespace NabdCare.IntegrationTests.Helpers;

/// <summary>
/// Helper methods for authentication in tests.
/// Author: Rabee-Qabaha
/// Updated: 2025-10-23 20:30:35 UTC
/// </summary>
public static class AuthHelper
{
    public static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var loginRequest = new LoginRequestDto
        {
            Email = email,
            Password = password
        };

        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        return authResponse?.AccessToken ?? throw new Exception("Login failed");
    }

    public static async Task<(string SuperAdminToken, string ClinicAdminToken, string DoctorToken)> GetAllTokensAsync(HttpClient client)
    {
        var superAdminToken = await LoginAsync(client, "sadmin@nabd.care", "Admin@123!");
        var clinicAdminToken = await LoginAsync(client, "cadmin@nabd.care", "Admin@123!");
        var doctorToken = await LoginAsync(client, "dadmin@nabd.care", "Admin@123!");

        return (superAdminToken, clinicAdminToken, doctorToken);
    }
}