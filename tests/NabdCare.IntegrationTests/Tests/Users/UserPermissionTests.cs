using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NabdCare.Application.DTOs.Users;
using NabdCare.IntegrationTests.Helpers;
using NabdCare.IntegrationTests.TestFixtures;

namespace NabdCare.IntegrationTests.Tests.Users;

/// <summary>
/// Comprehensive user permission tests covering all CRUD operations and access scenarios.
/// Author: Rabee-Qabaha
/// Updated: 2025-01-24 21:22:00 UTC
/// </summary>
[Collection("IntegrationTests")]
public class UserPermissionTests : IClassFixture<NabdCareWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly NabdCareWebApplicationFactory _factory;

    public UserPermissionTests(NabdCareWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region GET /api/users - View All Users

    [Fact]
    public async Task GetAllUsers_AsSuperAdmin_ReturnsAllUsers()
    {
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.SuperAdminEmail);
        var response = await _client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await response.Content.ReadFromJsonAsync<IEnumerable<UserResponseDto>>();
        users.Should().NotBeNull();
        users.Should().HaveCountGreaterThan(5);
        users.Should().Contain(u => u.Email == TestDataHelper.SuperAdminEmail);
    }

    [Fact]
    public async Task GetAllUsers_AsClinicAdmin_ReturnsOnlyClinicUsers()
    {
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.ClinicAdminEmail);
        var response = await _client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await response.Content.ReadFromJsonAsync<IEnumerable<UserResponseDto>>();
        users.Should().NotBeNull();
        users.Should().NotContain(u => u.Email == TestDataHelper.SuperAdminEmail);
        users.Should().Contain(u => u.Email == TestDataHelper.DoctorEmail);
    }

    [Fact]
    public async Task GetAllUsers_AsDoctor_ReturnsForbidden()
    {
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.DoctorEmail);
        var response = await _client.GetAsync("/api/users");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllUsers_Unauthenticated_ReturnsUnauthorized()
    {
        TestDataHelper.ClearAuthentication(_client);
        var response = await _client.GetAsync("/api/users");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/users/me

    [Theory]
    [InlineData(TestDataHelper.SuperAdminEmail)]
    [InlineData(TestDataHelper.ClinicAdminEmail)]
    [InlineData(TestDataHelper.DoctorEmail)]
    public async Task GetCurrentUser_AsAuthenticatedUser_ReturnsOwnProfile(string email)
    {
        await TestDataHelper.AuthenticateAs(_client, email);
        var response = await _client.GetAsync("/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserResponseDto>();
        user.Should().NotBeNull();
        user!.Email.Should().Be(email);
    }

    #endregion

    #region POST /api/users - Create User

    [Fact]
    public async Task CreateUser_AsSuperAdmin_Succeeds()
    {
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.SuperAdminEmail);

        var newUser = new CreateUserRequestDto
        {
            Email = "newuser@test.com",
            FullName = "New Test User",
            Password = TestDataHelper.NewUserPassword, // ✅ FIXED
            RoleId = _factory.DoctorRoleId,
            ClinicId = _factory.ClinicId
        };

        var response = await _client.PostAsJsonAsync("/api/users", newUser);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateUser_AsClinicAdmin_CanCreateClinicUser()
    {
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.ClinicAdminEmail);

        var newUser = new CreateUserRequestDto
        {
            Email = "newdoctor@test.com",
            FullName = "New Doctor",
            Password = TestDataHelper.NewUserPassword,
            RoleId = _factory.DoctorRoleId,
            ClinicId = _factory.ClinicId
        };

        var response = await _client.PostAsJsonAsync("/api/users", newUser);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateUser_AsDoctor_ReturnsForbidden()
    {
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.DoctorEmail);

        var newUser = new CreateUserRequestDto
        {
            Email = "unauthorized@test.com",
            FullName = "Unauthorized User",
            Password = TestDataHelper.NewUserPassword,
            RoleId = _factory.DoctorRoleId,
            ClinicId = _factory.ClinicId
        };

        var response = await _client.PostAsJsonAsync("/api/users", newUser);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region PUT /api/users/{id}

    [Fact]
    public async Task UpdateUser_AsSuperAdmin_Succeeds()
    {
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.SuperAdminEmail);

        var usersResponse = await _client.GetAsync("/api/users");
        var users = await usersResponse.Content.ReadFromJsonAsync<IEnumerable<UserResponseDto>>();
        var userToUpdate = users!.First(u => u.Email == TestDataHelper.NurseEmail);

        var updateDto = new UpdateUserRequestDto
        {
            FullName = "Updated Nurse Name",
            RoleId = userToUpdate.RoleId,
            IsActive = true
        };

        var response = await _client.PutAsJsonAsync($"/api/users/{userToUpdate.Id}", updateDto);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region DELETE /api/users/{id}

    [Fact]
    public async Task DeleteUser_AsSuperAdmin_Succeeds()
    {
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.SuperAdminEmail);

        var newUser = new CreateUserRequestDto
        {
            Email = "todelete@test.com",
            FullName = "To Delete",
            Password = TestDataHelper.NewUserPassword, // ✅ FIXED
            RoleId = _factory.DoctorRoleId,
            ClinicId = _factory.ClinicId
        };
        var createResponse = await _client.PostAsJsonAsync("/api/users", newUser);
        var created = await createResponse.Content.ReadFromJsonAsync<UserResponseDto>();

        var response = await _client.DeleteAsync($"/api/users/{created!.Id}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteUser_AsDoctor_ReturnsForbidden()
    {
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.DoctorEmail);
        var response = await _client.DeleteAsync($"/api/users/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Activate/Deactivate

    [Fact]
    public async Task ActivateUser_AsSuperAdmin_Succeeds()
    {
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.SuperAdminEmail);

        var newUser = new CreateUserRequestDto
        {
            Email = "inactive@test.com",
            FullName = "Inactive User",
            Password = TestDataHelper.NewUserPassword, // ✅ FIXED
            RoleId = _factory.DoctorRoleId,
            ClinicId = _factory.ClinicId
        };
        var createResponse = await _client.PostAsJsonAsync("/api/users", newUser);
        var created = await createResponse.Content.ReadFromJsonAsync<UserResponseDto>();

        await _client.PutAsync($"/api/users/{created!.Id}/deactivate", null);
        var response = await _client.PutAsync($"/api/users/{created.Id}/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeactivateUser_AsSuperAdmin_Succeeds()
    {
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.SuperAdminEmail);

        var newUser = new CreateUserRequestDto
        {
            Email = "active@test.com",
            FullName = "Active User",
            Password = TestDataHelper.NewUserPassword, // ✅ FIXED
            RoleId = _factory.DoctorRoleId,
            ClinicId = _factory.ClinicId
        };
        var createResponse = await _client.PostAsJsonAsync("/api/users", newUser);
        var created = await createResponse.Content.ReadFromJsonAsync<UserResponseDto>();

        var response = await _client.PutAsync($"/api/users/{created!.Id}/deactivate", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion
    
    #region Password Change Edge Cases

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_ReturnsBadRequest()
    {
        // Arrange
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.SuperAdminEmail);
    
        var usersResponse = await _client.GetAsync("/api/users");
        var users = await usersResponse.Content.ReadFromJsonAsync<IEnumerable<UserResponseDto>>();
        var user = users!.First(u => u.Email == TestDataHelper.NurseEmail);

        var changePasswordDto = new ChangePasswordRequestDto
        {
            CurrentPassword = "WrongPassword@123!",
            NewPassword = TestDataHelper.NewUserPassword
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/users/{user.Id}/change-password", changePasswordDto);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ResetPassword_ForInactiveUser_MaySucceed()
    {
        // Arrange
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.SuperAdminEmail);

        // Create and deactivate a user
        var newUser = new CreateUserRequestDto
        {
            Email = "inactive.user@test.com",
            FullName = "Inactive User",
            Password = TestDataHelper.NewUserPassword,
            RoleId = _factory.DoctorRoleId,
            ClinicId = _factory.ClinicId
        };
        var createResponse = await _client.PostAsJsonAsync("/api/users", newUser);
        var created = await createResponse.Content.ReadFromJsonAsync<UserResponseDto>();

        await _client.PutAsync($"/api/users/{created!.Id}/deactivate", null);

        var resetDto = new ResetPasswordRequestDto
        {
            NewPassword = "NewPassword@123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/users/{created.Id}/reset-password", resetDto);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    #endregion
}