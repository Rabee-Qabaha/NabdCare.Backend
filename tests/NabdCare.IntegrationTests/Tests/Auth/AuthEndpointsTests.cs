using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NabdCare.Application.DTOs.Auth;
using NabdCare.Application.DTOs.Users;
using NabdCare.IntegrationTests.Helpers;
using NabdCare.IntegrationTests.TestFixtures;

namespace NabdCare.IntegrationTests.Tests.Auth;

/// <summary>
/// Comprehensive authentication endpoint tests.
/// Author: Rabee-Qabaha
/// Created: 2025-10-24 22:11:00 UTC
/// </summary>
[Collection("IntegrationTests")]
public class AuthEndpointsTests : IClassFixture<NabdCareWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly NabdCareWebApplicationFactory _factory;

    public AuthEndpointsTests(NabdCareWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region POST /api/auth/login - Login Tests

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsAccessToken()
    {
        // Arrange
        var loginDto = new LoginRequestDto
        {
            Email = TestDataHelper.SuperAdminEmail,
            Password = TestDataHelper.TestPassword
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        authResponse.Should().NotBeNull();
        authResponse!.AccessToken.Should().NotBeNullOrEmpty();
        
        // Should set refresh token cookie
        response.Headers.TryGetValues("Set-Cookie", out var cookies);
        cookies.Should().NotBeNull();
        cookies.Should().Contain(c => c.Contains("refreshToken"));
    }

    [Theory]
    [InlineData(TestDataHelper.SuperAdminEmail)]
    [InlineData(TestDataHelper.ClinicAdminEmail)]
    [InlineData(TestDataHelper.DoctorEmail)]
    [InlineData(TestDataHelper.NurseEmail)]
    public async Task Login_WithDifferentValidUsers_Succeeds(string email)
    {
        // Arrange
        var loginDto = new LoginRequestDto
        {
            Email = email,
            Password = TestDataHelper.TestPassword
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        authResponse!.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginRequestDto
        {
            Email = TestDataHelper.SuperAdminEmail,
            Password = "WrongPassword@123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginRequestDto
        {
            Email = "nonexistent@test.com",
            Password = TestDataHelper.TestPassword
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithEmptyEmail_ReturnsBadRequest()
    {
        // Arrange
        var loginDto = new LoginRequestDto
        {
            Email = "",
            Password = TestDataHelper.TestPassword
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Email and Password are required");
    }

    [Fact]
    public async Task Login_WithEmptyPassword_ReturnsBadRequest()
    {
        // Arrange
        var loginDto = new LoginRequestDto
        {
            Email = TestDataHelper.SuperAdminEmail,
            Password = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Email and Password are required");
    }

    [Fact]
    public async Task Login_WithNullEmail_ReturnsBadRequest()
    {
        // Arrange
        var loginDto = new LoginRequestDto
        {
            Email = null!,
            Password = TestDataHelper.TestPassword
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithInvalidEmailFormat_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginRequestDto
        {
            Email = "not-an-email",
            Password = TestDataHelper.TestPassword
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ForDeactivatedUser_ReturnsUnauthorized()
    {
        // Arrange
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.SuperAdminEmail);

        // Create and deactivate a user
        var newUser = new CreateUserRequestDto
        {
            Email = "deactivated.user@test.com",
            FullName = "Deactivated User",
            Password = TestDataHelper.NewUserPassword,
            RoleId = _factory.DoctorRoleId,
            ClinicId = _factory.ClinicId
        };
        var createResponse = await _client.PostAsJsonAsync("/api/users", newUser);
        var created = await createResponse.Content.ReadFromJsonAsync<UserResponseDto>();
        await _client.PutAsync($"/api/users/{created!.Id}/deactivate", null);

        // Clear authentication
        TestDataHelper.ClearAuthentication(_client);

        var loginDto = new LoginRequestDto
        {
            Email = "deactivated.user@test.com",
            Password = TestDataHelper.NewUserPassword
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/auth/refresh - Refresh Token Tests

    [Fact]
    public async Task RefreshToken_WithValidRefreshToken_ReturnsNewAccessToken()
    {
        // Arrange - First login to get refresh token
        var loginDto = new LoginRequestDto
        {
            Email = TestDataHelper.SuperAdminEmail,
            Password = TestDataHelper.TestPassword
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        
        // Extract refresh token from cookie
        loginResponse.Headers.TryGetValues("Set-Cookie", out var cookies);
        var refreshTokenCookie = cookies!.FirstOrDefault(c => c.Contains("refreshToken"));
        refreshTokenCookie.Should().NotBeNull();

        // Add refresh token to request
        var cookieValue = ExtractCookieValue(refreshTokenCookie!);
        _client.DefaultRequestHeaders.Add("Cookie", $"refreshToken={cookieValue}");

        // Act
        var response = await _client.PostAsync("/api/auth/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        authResponse!.AccessToken.Should().NotBeNullOrEmpty();
        
        // Should rotate refresh token
        response.Headers.TryGetValues("Set-Cookie", out var newCookies);
        newCookies.Should().Contain(c => c.Contains("refreshToken"));
    }

    [Fact]
    public async Task RefreshToken_WithoutRefreshToken_ReturnsUnauthorized()
    {
        // Arrange - No refresh token cookie

        // Act
        var response = await _client.PostAsync("/api/auth/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Refresh token not found");
    }

    [Fact]
    public async Task RefreshToken_WithInvalidRefreshToken_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Cookie", "refreshToken=invalid-token-12345");

        // Act
        var response = await _client.PostAsync("/api/auth/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_WithExpiredRefreshToken_ReturnsUnauthorized()
    {
        // Arrange - Use a token that's already expired or revoked
        var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjB9.invalid";
        _client.DefaultRequestHeaders.Add("Cookie", $"refreshToken={expiredToken}");

        // Act
        var response = await _client.PostAsync("/api/auth/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_AfterLogout_ReturnsUnauthorized()
    {
        // Arrange - Login first
        var loginDto = new LoginRequestDto
        {
            Email = TestDataHelper.SuperAdminEmail,
            Password = TestDataHelper.TestPassword
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        
        // Set authorization header
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);

        // Extract and set refresh token
        loginResponse.Headers.TryGetValues("Set-Cookie", out var cookies);
        var refreshTokenCookie = cookies!.FirstOrDefault(c => c.Contains("refreshToken"));
        var cookieValue = ExtractCookieValue(refreshTokenCookie!);
        _client.DefaultRequestHeaders.Add("Cookie", $"refreshToken={cookieValue}");

        // Logout
        await _client.PostAsync("/api/auth/logout", null);

        // Act - Try to refresh after logout
        var response = await _client.PostAsync("/api/auth/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/auth/logout - Logout Tests

    [Fact]
    public async Task Logout_WithValidAuthentication_RevokesRefreshToken()
    {
        // Arrange - Login first
        var loginDto = new LoginRequestDto
        {
            Email = TestDataHelper.SuperAdminEmail,
            Password = TestDataHelper.TestPassword
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        
        // Set authorization
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);

        // Extract refresh token
        loginResponse.Headers.TryGetValues("Set-Cookie", out var cookies);
        var refreshTokenCookie = cookies!.FirstOrDefault(c => c.Contains("refreshToken"));
        var cookieValue = ExtractCookieValue(refreshTokenCookie!);
        _client.DefaultRequestHeaders.Add("Cookie", $"refreshToken={cookieValue}");

        // Act
        var response = await _client.PostAsync("/api/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // Should delete refresh token cookie
        response.Headers.TryGetValues("Set-Cookie", out var logoutCookies);
        if (logoutCookies != null)
        {
            logoutCookies.Should().Contain(c => c.Contains("refreshToken") && c.Contains("expires="));
        }
    }

    [Fact]
    public async Task Logout_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange - No authentication

        // Act
        var response = await _client.PostAsync("/api/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WithoutRefreshToken_StillSucceeds()
    {
        // Arrange - Authenticate but no refresh token
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.SuperAdminEmail);

        // Act
        var response = await _client.PostAsync("/api/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Logout_WithInvalidAccessToken_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid.token.here");

        // Act
        var response = await _client.PostAsync("/api/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Integration Flow Tests

[Fact]
public async Task AuthFlow_LoginRefreshLogout_WorksEndToEnd()
{
    // Step 1: Login
    var loginDto = new LoginRequestDto
    {
        Email = TestDataHelper.ClinicAdminEmail,
        Password = TestDataHelper.TestPassword
    };
    var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
    loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    
    var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
    authResponse!.AccessToken.Should().NotBeNullOrEmpty();

    // Step 2: Extract original refresh token
    loginResponse.Headers.TryGetValues("Set-Cookie", out var loginCookies);
    var refreshTokenCookie = loginCookies!.FirstOrDefault(c => c.Contains("refreshToken"));
    var originalRefreshToken = ExtractCookieValue(refreshTokenCookie!);

    // Step 3: Use access token to access protected resource
    _client.DefaultRequestHeaders.Authorization = 
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.AccessToken);
    var meResponse = await _client.GetAsync("/api/users/me");
    meResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    // Step 4: Refresh token (this will rotate the refresh token)
    _client.DefaultRequestHeaders.Add("Cookie", $"refreshToken={originalRefreshToken}");
    var refreshResponse = await _client.PostAsync("/api/auth/refresh", null);
    refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    
    var newAuthResponse = await refreshResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
    newAuthResponse!.AccessToken.Should().NotBeNullOrEmpty();
    newAuthResponse.AccessToken.Should().MatchRegex(@"^eyJ[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+$");

    // Step 5: Extract NEW refresh token from refresh response
    refreshResponse.Headers.TryGetValues("Set-Cookie", out var refreshCookies);
    var newRefreshCookie = refreshCookies!.FirstOrDefault(c => c.Contains("refreshToken"));
    var newRefreshToken = ExtractCookieValue(newRefreshCookie!);

    // Step 6: Update client to use new tokens
    _client.DefaultRequestHeaders.Authorization = 
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newAuthResponse.AccessToken);
    _client.DefaultRequestHeaders.Remove("Cookie");
    _client.DefaultRequestHeaders.Add("Cookie", $"refreshToken={newRefreshToken}");

    // Step 7: Verify new token works
    var me2Response = await _client.GetAsync("/api/users/me");
    me2Response.StatusCode.Should().Be(HttpStatusCode.OK);

    // Step 8: Logout (should revoke the NEW refresh token)
    var logoutResponse = await _client.PostAsync("/api/auth/logout", null);
    logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

    // Step 9: Try to use the refresh token after logout - should fail
    var finalRefreshResponse = await _client.PostAsync("/api/auth/refresh", null);
    finalRefreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

    // Step 10: Verify old refresh token is also invalid (was rotated)
    _client.DefaultRequestHeaders.Remove("Cookie");
    _client.DefaultRequestHeaders.Add("Cookie", $"refreshToken={originalRefreshToken}");
    var oldRefreshResponse = await _client.PostAsync("/api/auth/refresh", null);
    oldRefreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}

[Fact]
public async Task MultipleLogins_WithSameUser_AreSuccessful()
{
    // Arrange
    var loginDto = new LoginRequestDto
    {
        Email = TestDataHelper.DoctorEmail,
        Password = TestDataHelper.TestPassword
    };

    // Act - Login twice
    var response1 = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
    var auth1 = await response1.Content.ReadFromJsonAsync<AuthResponseDto>();

    // ✅ ADD: Small delay to ensure different timestamps
    await Task.Delay(1100); // Wait 1.1 seconds to ensure different 'iat' and 'exp' claims

    var response2 = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
    var auth2 = await response2.Content.ReadFromJsonAsync<AuthResponseDto>();

    // Assert
    response1.StatusCode.Should().Be(HttpStatusCode.OK);
    response2.StatusCode.Should().Be(HttpStatusCode.OK);
    
    auth1!.AccessToken.Should().NotBeNullOrEmpty();
    auth2!.AccessToken.Should().NotBeNullOrEmpty();
    
    // ✅ FIXED: Tokens should be different after delay (different timestamps)
    auth1.AccessToken.Should().NotBe(auth2.AccessToken, 
        "tokens generated at different times should have different 'iat' and 'exp' claims");
    
    // Both tokens should be valid JWT format
    auth1.AccessToken.Should().MatchRegex(@"^eyJ[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+$");
    auth2.AccessToken.Should().MatchRegex(@"^eyJ[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+$");
}

    #endregion

    #region Security Tests

    [Fact]
    public async Task Login_RefreshTokenCookie_HasSecureAttributes()
    {
        // Arrange
        var loginDto = new LoginRequestDto
        {
            Email = TestDataHelper.SuperAdminEmail,
            Password = TestDataHelper.TestPassword
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        response.Headers.TryGetValues("Set-Cookie", out var cookies);
        var refreshTokenCookie = cookies!.FirstOrDefault(c => c.Contains("refreshToken"));
        
        refreshTokenCookie.Should().NotBeNull();
        refreshTokenCookie.Should().Contain("httponly", "Cookie should be HttpOnly");
        refreshTokenCookie.Should().Contain("secure", "Cookie should be Secure");
        refreshTokenCookie.Should().Contain("samesite=strict", "Cookie should be SameSite=Strict");
    }

    [Fact]
    public async Task AccessToken_CanBeUsedForAuthentication()
    {
        // Arrange
        var loginDto = new LoginRequestDto
        {
            Email = TestDataHelper.SuperAdminEmail,
            Password = TestDataHelper.TestPassword
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        // Act - Use token to access protected endpoint
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);
        var response = await _client.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserResponseDto>();
        user!.Email.Should().Be(TestDataHelper.SuperAdminEmail);
    }

    #endregion

    #region Helper Methods

    private static string ExtractCookieValue(string cookieHeader)
    {
        return TestDataHelper.ExtractCookieValue(cookieHeader, "refreshToken");
    }

    #endregion
    
    
}