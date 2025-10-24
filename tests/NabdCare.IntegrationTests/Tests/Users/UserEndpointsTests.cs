using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NabdCare.Application.DTOs.Users;
using NabdCare.IntegrationTests.Helpers;
using NabdCare.IntegrationTests.TestFixtures;
using Xunit;

namespace NabdCare.IntegrationTests.Tests.Users;

/// <summary>
/// Integration tests for User endpoints with permission validation.
/// Author: Rabee-Qabaha
/// Updated: 2025-10-23 20:30:50 UTC
/// </summary>
public class UserEndpointsTests : IClassFixture<NabdCareWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UserEndpointsTests(NabdCareWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllUsers_AsSuperAdmin_ReturnsAllUsers()
    {
        // Arrange
        var (superAdminToken, _, _) = await AuthHelper.GetAllTokensAsync(_client);

        // Act
        var response = await _client.GetAuthenticatedAsync("/api/users", superAdminToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await response.Content.ReadFromJsonAsync<List<UserResponseDto>>();
        users.Should().NotBeNull();
        users.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task GetAllUsers_AsClinicAdmin_ReturnsOnlyClinicUsers()
    {
        // Arrange
        var (_, clinicAdminToken, _) = await AuthHelper.GetAllTokensAsync(_client);

        // Act
        var response = await _client.GetAuthenticatedAsync("/api/users", clinicAdminToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await response.Content.ReadFromJsonAsync<List<UserResponseDto>>();
        users.Should().NotBeNull();
        users.Should().HaveCountGreaterThanOrEqualTo(2);
        users.Should().NotContain(u => u.Email == "sadmin@nabd.care"); // SuperAdmin not in list
    }

    [Fact]
    public async Task GetAllUsers_AsDoctor_ReturnsForbidden()
    {
        // Arrange
        var (_, _, doctorToken) = await AuthHelper.GetAllTokensAsync(_client);

        // Act
        var response = await _client.GetAuthenticatedAsync("/api/users", doctorToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetCurrentUser_AsSuperAdmin_ReturnsOwnDetails()
    {
        // Arrange
        var (superAdminToken, _, _) = await AuthHelper.GetAllTokensAsync(_client);

        // Act
        var response = await _client.GetAuthenticatedAsync("/api/users/me", superAdminToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserResponseDto>();
        user.Should().NotBeNull();
        user!.Email.Should().Be("sadmin@nabd.care");
    }

    [Fact]
    public async Task GetCurrentUser_AsClinicAdmin_ReturnsOwnDetails()
    {
        // Arrange
        var (_, clinicAdminToken, _) = await AuthHelper.GetAllTokensAsync(_client);

        // Act
        var response = await _client.GetAuthenticatedAsync("/api/users/me", clinicAdminToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserResponseDto>();
        user.Should().NotBeNull();
        user!.Email.Should().Be("cadmin@nabd.care");  // ✅ FIXED
    }

    [Fact]
    public async Task GetCurrentUser_AsDoctor_ReturnsOwnDetails()
    {
        // Arrange
        var (_, _, doctorToken) = await AuthHelper.GetAllTokensAsync(_client);

        // Act
        var response = await _client.GetAuthenticatedAsync("/api/users/me", doctorToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserResponseDto>();
        user.Should().NotBeNull();
        user!.Email.Should().Be("dadmin@nabd.care");  // ✅ FIXED
    }
}