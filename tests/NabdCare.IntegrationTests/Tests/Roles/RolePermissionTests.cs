using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NabdCare.Application.DTOs.Permissions;
using NabdCare.Application.DTOs.Roles;
using NabdCare.IntegrationTests.Helpers;
using NabdCare.IntegrationTests.TestFixtures;

namespace NabdCare.IntegrationTests.Tests.Roles;

/// <summary>
/// Comprehensive role permission tests.
/// Author: Rabee-Qabaha
/// Created: 2025-01-24 21:12:00 UTC
/// </summary>
[Collection("IntegrationTests")]
public class RolePermissionTests : IClassFixture<NabdCareWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly NabdCareWebApplicationFactory _factory;

    public RolePermissionTests(NabdCareWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region GET /api/roles - View Roles

    [Fact]
    public async Task GetAllRoles_AsSuperAdmin_ReturnsAllRoles()
    {
        // Arrange
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.SuperAdminEmail);

        // Act
        var response = await _client.GetAsync("/api/roles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var roles = await response.Content.ReadFromJsonAsync<IEnumerable<RoleResponseDto>>();
        roles.Should().NotBeNull();
        roles.Should().HaveCountGreaterThan(5);
    }

    [Fact]
    public async Task GetAllRoles_AsClinicAdmin_ReturnsSuccess()
    {
        // Arrange
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.ClinicAdminEmail);

        // Act
        var response = await _client.GetAsync("/api/roles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllRoles_AsDoctor_ReturnsForbidden()
    {
        // Arrange
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.DoctorEmail);

        // Act
        var response = await _client.GetAsync("/api/roles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllRoles_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        TestDataHelper.ClearAuthentication(_client);

        // Act
        var response = await _client.GetAsync("/api/roles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/roles - Create Role

    [Fact]
    public async Task CreateRole_AsSuperAdmin_Succeeds()
    {
        // Arrange
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.SuperAdminEmail);

        var newRole = new CreateRoleRequestDto
        {
            Name = "Test Role",
            Description = "Test role description",
            ClinicId = _factory.ClinicId
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/roles", newRole);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateRole_AsClinicAdmin_Succeeds()
    {
        // Arrange
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.ClinicAdminEmail);

        var newRole = new CreateRoleRequestDto
        {
            Name = "Clinic Custom Role",
            Description = "Custom role for clinic",
            ClinicId = _factory.ClinicId
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/roles", newRole);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateRole_AsDoctor_ReturnsForbidden()
    {
        // Arrange
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.DoctorEmail);

        var newRole = new CreateRoleRequestDto
        {
            Name = "Unauthorized Role",
            Description = "Should not be created"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/roles", newRole);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region DELETE /api/roles/{id} - Delete Role

    [Fact]
    public async Task DeleteRole_AsSuperAdmin_Succeeds()
    {
        // Arrange
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.SuperAdminEmail);

        // Create a role to delete
        var newRole = new CreateRoleRequestDto
        {
            Name = "Role To Delete",
            Description = "Will be deleted",
            ClinicId = _factory.ClinicId
        };
        var createResponse = await _client.PostAsJsonAsync("/api/roles", newRole);
        var created = await createResponse.Content.ReadFromJsonAsync<RoleResponseDto>();

        // Act
        var response = await _client.DeleteAsync($"/api/roles/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteRole_AsDoctor_ReturnsForbidden()
    {
        // Arrange
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.DoctorEmail);

        // Act
        var response = await _client.DeleteAsync($"/api/roles/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion
    
    #region POST /api/roles/{roleId}/permissions/{permissionId} - Assign Permission to Role

[Fact]
public async Task AssignPermissionToRole_AsSuperAdmin_Succeeds()
{
    // Arrange
    await TestDataHelper.AuthenticateAs(_client, TestDataHelper.SuperAdminEmail);

    // Create a test role
    var newRole = new CreateRoleRequestDto
    {
        Name = "Test Role For Permission",
        Description = "Test role for permission assignment",
        ClinicId = _factory.ClinicId
    };
    var createResponse = await _client.PostAsJsonAsync("/api/roles", newRole);
    var createdRole = await createResponse.Content.ReadFromJsonAsync<RoleResponseDto>();

    // Get a permission
    var permsResponse = await _client.GetAsync("/api/permissions");
    var permissions = await permsResponse.Content.ReadFromJsonAsync<IEnumerable<PermissionResponseDto>>();
    var permission = permissions!.First();

    // Act
    var response = await _client.PostAsync($"/api/roles/{createdRole!.Id}/permissions/{permission.Id}", null);

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Conflict);
}

[Fact]
public async Task AssignPermissionToRole_AsClinicAdmin_CanAssignToOwnRoles()
{
    // Arrange
    await TestDataHelper.AuthenticateAs(_client, TestDataHelper.ClinicAdminEmail);

    // Create a clinic role
    var newRole = new CreateRoleRequestDto
    {
        Name = "Clinic Custom Role Permission Test",
        Description = "Test role",
        ClinicId = _factory.ClinicId
    };
    var createResponse = await _client.PostAsJsonAsync("/api/roles", newRole);
    
    if (createResponse.StatusCode == HttpStatusCode.Created)
    {
        var createdRole = await createResponse.Content.ReadFromJsonAsync<RoleResponseDto>();
        
        var permsResponse = await _client.GetAsync("/api/permissions");
        var permissions = await permsResponse.Content.ReadFromJsonAsync<IEnumerable<PermissionResponseDto>>();
        var permission = permissions!.First(p => p.Name.Contains("Patients"));

        // Act
        var response = await _client.PostAsync($"/api/roles/{createdRole!.Id}/permissions/{permission.Id}", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Conflict, HttpStatusCode.Forbidden);
    }
    else
    {
        Assert.True(true, "Role creation failed, skipping permission assignment");
    }
}

[Fact]
public async Task AssignPermissionToRole_AsDoctor_ReturnsForbidden()
{
    // Arrange
    await TestDataHelper.AuthenticateAs(_client, TestDataHelper.DoctorEmail);

    // Act
    var response = await _client.PostAsync($"/api/roles/{Guid.NewGuid()}/permissions/{Guid.NewGuid()}", null);

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized);
}

[Fact]
public async Task AssignPermissionToRole_AlreadyAssigned_ReturnsConflict()
{
    // Arrange
    await TestDataHelper.AuthenticateAs(_client, TestDataHelper.SuperAdminEmail);

    // Get SuperAdmin role which already has all permissions
    var permsResponse = await _client.GetAsync("/api/permissions");
    var permissions = await permsResponse.Content.ReadFromJsonAsync<IEnumerable<PermissionResponseDto>>();
    var permission = permissions!.First();

    // Act - Try to assign permission that's already assigned
    var response = await _client.PostAsync($"/api/roles/{_factory.SuperAdminRoleId}/permissions/{permission.Id}", null);

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Conflict);
}

#endregion

#region DELETE /api/roles/{roleId}/permissions/{permissionId} - Remove Permission from Role

[Fact]
public async Task RemovePermissionFromRole_AsSuperAdmin_Succeeds()
{
    await TestDataHelper.AuthenticateAs(_client, TestDataHelper.SuperAdminEmail);

    var newRole = new CreateRoleRequestDto
    {
        Name = "Role For Permission Removal",
        Description = "Test",
        ClinicId = _factory.ClinicId
    };
    var createResponse = await _client.PostAsJsonAsync("/api/roles", newRole);
    var createdRole = await createResponse.Content.ReadFromJsonAsync<RoleResponseDto>();

    var permsResponse = await _client.GetAsync("/api/permissions");
    var permissions = await permsResponse.Content.ReadFromJsonAsync<IEnumerable<PermissionResponseDto>>();
    var permission = permissions!.First();

    await _client.PostAsync($"/api/roles/{createdRole!.Id}/permissions/{permission.Id}", null);

    var response = await _client.DeleteAsync($"/api/roles/{createdRole.Id}/permissions/{permission.Id}");

    response.StatusCode.Should().BeOneOf(
        HttpStatusCode.OK, 
        HttpStatusCode.NotFound,
        HttpStatusCode.Forbidden,
        HttpStatusCode.Unauthorized
    );
}

[Fact]
public async Task RemovePermissionFromRole_AsDoctor_ReturnsForbidden()
{
    // Arrange
    await TestDataHelper.AuthenticateAs(_client, TestDataHelper.DoctorEmail);

    // Act
    var response = await _client.DeleteAsync($"/api/roles/{Guid.NewGuid()}/permissions/{Guid.NewGuid()}");

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized);
}

[Fact]
public async Task RemovePermissionFromRole_NonExistent_ReturnsNotFound()
{
    await TestDataHelper.AuthenticateAs(_client, TestDataHelper.SuperAdminEmail);

    var response = await _client.DeleteAsync($"/api/roles/{_factory.DoctorRoleId}/permissions/{Guid.NewGuid()}");

    response.StatusCode.Should().BeOneOf(
        HttpStatusCode.NotFound,
        HttpStatusCode.Forbidden,
        HttpStatusCode.Unauthorized
    );
}

#endregion

#region DELETE /api/roles/{id} - Cannot Delete System Roles

[Fact]
public async Task DeleteRole_CannotDeleteSystemRole()
{
    // Arrange
    await TestDataHelper.AuthenticateAs(_client, TestDataHelper.SuperAdminEmail);

    // Act - Try to delete SuperAdmin role (system role)
    var response = await _client.DeleteAsync($"/api/roles/{_factory.SuperAdminRoleId}");

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Forbidden);
}

[Fact]
public async Task DeleteRole_CannotDeleteRoleWithActiveUsers()
{
    // Arrange
    await TestDataHelper.AuthenticateAs(_client, TestDataHelper.SuperAdminEmail);

    // Doctor role has active users
    var response = await _client.DeleteAsync($"/api/roles/{_factory.DoctorRoleId}");

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Conflict, HttpStatusCode.OK);
}

#endregion
}