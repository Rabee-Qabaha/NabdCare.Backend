using System.Net.Http.Headers;
using System.Net.Http.Json;
using NabdCare.Application.DTOs.Auth;
using NabdCare.Application.DTOs.Users;

namespace NabdCare.IntegrationTests.Helpers;

/// <summary>
/// Helper class for test data and common operations.
/// Author: Rabee-Qabaha
/// Updated: 2025-10-24 22:13:31 UTC
/// </summary>
public static class TestDataHelper
{
    public const string SuperAdminEmail = "sadmin@nabd.care";
    public const string ClinicAdminEmail = "cadmin@nabd.care";
    public const string DoctorEmail = "dadmin@nabd.care";
    public const string NurseEmail = "nurse@nabd.care";
    public const string ReceptionistEmail = "receptionist@nabd.care";
    public const string TestPassword = "Admin@123!";
    public const string NewUserPassword = "TestPass@123!";

    /// <summary>
    /// Get authentication token for a user
    /// </summary>
    public static async Task<string> GetTokenAsync(HttpClient client, string email, string password = TestPassword)
    {
        var loginDto = new LoginRequestDto { Email = email, Password = password };
        var response = await client.PostAsJsonAsync("/api/auth/login", loginDto);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        return result!.AccessToken;
    }

    /// <summary>
    /// Set Bearer token on HttpClient
    /// </summary>
    public static void SetBearerToken(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Authenticate client as a specific user (sets Bearer token)
    /// </summary>
    public static async Task AuthenticateAs(HttpClient client, string email, string password = TestPassword)
    {
        var token = await GetTokenAsync(client, email, password);
        SetBearerToken(client, token);
    }

    /// <summary>
    /// Clear authentication from client
    /// </summary>
    public static void ClearAuthentication(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = null;
        client.DefaultRequestHeaders.Remove("Cookie");
    }

    /// <summary>
    /// Login and get full auth response (without setting it on client)
    /// Useful for testing auth flows
    /// </summary>
    public static async Task<AuthResponseDto> LoginAsync(HttpClient client, string email, string password = TestPassword)
    {
        var loginDto = new LoginRequestDto { Email = email, Password = password };
        var response = await client.PostAsJsonAsync("/api/auth/login", loginDto);
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        return result!;
    }

    /// <summary>
    /// Extract cookie value from Set-Cookie header
    /// </summary>
    public static string ExtractCookieValue(string cookieHeader, string cookieName)
    {
        // Format: "cookieName=value; HttpOnly; Secure; ..."
        var parts = cookieHeader.Split(';');
        var cookiePart = parts[0].Trim();
        
        if (cookiePart.StartsWith($"{cookieName}="))
        {
            return cookiePart.Substring(cookieName.Length + 1);
        }
        
        return string.Empty;
    }
}