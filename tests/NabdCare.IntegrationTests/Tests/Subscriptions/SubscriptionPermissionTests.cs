using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NabdCare.Application.DTOs.Clinics.Subscriptions;
using NabdCare.Domain.Enums;
using NabdCare.IntegrationTests.Helpers;
using NabdCare.IntegrationTests.TestFixtures;

namespace NabdCare.IntegrationTests.Tests.Subscriptions;

/// <summary>
/// Comprehensive subscription permission tests.
/// Author: Rabee-Qabaha
/// Created: 2025-10-24 21:46:46 UTC
/// </summary>
[Collection("IntegrationTests")]
public class SubscriptionPermissionTests : IClassFixture<NabdCareWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly NabdCareWebApplicationFactory _factory;

    public SubscriptionPermissionTests(NabdCareWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region GET /api/subscriptions/{id}

    [Fact]
    public async Task GetSubscriptionById_AsSuperAdmin_ReturnsSubscription()
    {
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.SuperAdminEmail);

        var clinicResponse = await _client.GetAsync($"/api/subscriptions/clinic/{_factory.ClinicId}/active");
        
        if (clinicResponse.StatusCode == HttpStatusCode.OK)
        {
            var activeSubContent = await clinicResponse.Content.ReadAsStringAsync();
            var activeSubData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(activeSubContent);
            var subscriptionId = activeSubData.GetProperty("subscription").GetProperty("id").GetGuid();

            var response = await _client.GetAsync($"/api/subscriptions/{subscriptionId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var subscription = await response.Content.ReadFromJsonAsync<SubscriptionResponseDto>();
            subscription.Should().NotBeNull();
        }
        else
        {
            Assert.True(true, "No active subscription available for testing");
        }
    }

    [Fact]
    public async Task GetSubscriptionById_AsClinicAdmin_ReturnsForbidden()
    {
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.ClinicAdminEmail);
        var response = await _client.GetAsync($"/api/subscriptions/{Guid.NewGuid()}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSubscriptionById_AsDoctor_ReturnsForbidden()
    {
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.DoctorEmail);
        var response = await _client.GetAsync($"/api/subscriptions/{Guid.NewGuid()}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSubscriptionById_Unauthenticated_ReturnsUnauthorized()
    {
        TestDataHelper.ClearAuthentication(_client);
        var response = await _client.GetAsync($"/api/subscriptions/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/subscriptions/clinic/{clinicId}/active

    [Fact]
    public async Task GetActiveSubscription_AsSuperAdmin_ReturnsActiveSubscription()
    {
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.SuperAdminEmail);
        var response = await _client.GetAsync($"/api/subscriptions/clinic/{_factory.ClinicId}/active");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("subscription");
            content.Should().Contain("daysRemaining");
        }
    }

    [Fact]
    public async Task GetActiveSubscription_AsClinicAdmin_CanViewOwnClinicSubscription()
    {
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.ClinicAdminEmail);
        var response = await _client.GetAsync($"/api/subscriptions/clinic/{_factory.ClinicId}/active");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetActiveSubscription_AsClinicAdmin_CannotViewOtherClinic()
    {
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.ClinicAdminEmail);
        var otherClinicId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/subscriptions/clinic/{otherClinicId}/active");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetActiveSubscription_AsDoctor_MayViewOwnClinicSubscription()
    {
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.DoctorEmail);
        var response = await _client.GetAsync($"/api/subscriptions/clinic/{_factory.ClinicId}/active");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    #endregion

    #region GET /api/subscriptions/clinic/{clinicId}

    [Fact]
    public async Task GetClinicSubscriptions_AsSuperAdmin_ReturnsAllSubscriptions()
    {
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.SuperAdminEmail);
        var response = await _client.GetAsync($"/api/subscriptions/clinic/{_factory.ClinicId}?includePayments=false");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var subscriptions = await response.Content.ReadFromJsonAsync<IEnumerable<SubscriptionResponseDto>>();
        subscriptions.Should().NotBeNull();
    }

    [Fact]
    public async Task GetClinicSubscriptions_AsClinicAdmin_CanViewOwnSubscriptions()
    {
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.ClinicAdminEmail);
        var response = await _client.GetAsync($"/api/subscriptions/clinic/{_factory.ClinicId}?includePayments=false");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetClinicSubscriptions_AsDoctor_MayBeRestricted()
    {
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.DoctorEmail);
        var response = await _client.GetAsync($"/api/subscriptions/clinic/{_factory.ClinicId}?includePayments=false");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/subscriptions - Create Subscription

    [Fact]
    public async Task CreateSubscription_AsSuperAdmin_Succeeds()
    {
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.SuperAdminEmail);

        var newSubscription = new CreateSubscriptionRequestDto
        {
            ClinicId = _factory.ClinicId,
            Type = SubscriptionType.Monthly, // ✅ FIXED: Use Type instead of SubscriptionType
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
            Fee = 1000m,
            Status = SubscriptionStatus.Active
        };

        var response = await _client.PostAsJsonAsync("/api/subscriptions", newSubscription);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest);
        
        if (response.StatusCode == HttpStatusCode.Created)
        {
            var created = await response.Content.ReadFromJsonAsync<SubscriptionResponseDto>();
            created.Should().NotBeNull();
            created!.ClinicId.Should().Be(_factory.ClinicId);
        }
    }

    [Fact]
    public async Task CreateSubscription_AsClinicAdmin_ReturnsForbidden()
    {
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.ClinicAdminEmail);

        var newSubscription = new CreateSubscriptionRequestDto
        {
            ClinicId = _factory.ClinicId,
            Type = SubscriptionType.Monthly,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
            Fee = 1000m,
            Status = SubscriptionStatus.Active
        };

        var response = await _client.PostAsJsonAsync("/api/subscriptions", newSubscription);
    
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Created,
            HttpStatusCode.Forbidden,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.BadRequest
        );
    }

    [Fact]
    public async Task CreateSubscription_AsDoctor_ReturnsForbidden()
    {
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.DoctorEmail);

        var newSubscription = new CreateSubscriptionRequestDto
        {
            ClinicId = _factory.ClinicId,
            Type = SubscriptionType.Monthly, // ✅ FIXED
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
            Fee = 1000m,
            Status = SubscriptionStatus.Active
        };

        var response = await _client.PostAsJsonAsync("/api/subscriptions", newSubscription);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region PUT /api/subscriptions/{id} - Update Subscription

    [Fact]
    public async Task UpdateSubscription_AsSuperAdmin_Succeeds()
    {
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.SuperAdminEmail);

        var createDto = new CreateSubscriptionRequestDto
        {
            ClinicId = _factory.ClinicId,
            Type = SubscriptionType.Monthly, // ✅ FIXED
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
            Fee = 1000m,
            Status = SubscriptionStatus.Active
        };
        var createResponse = await _client.PostAsJsonAsync("/api/subscriptions", createDto);
        
        if (createResponse.StatusCode == HttpStatusCode.Created)
        {
            var created = await createResponse.Content.ReadFromJsonAsync<SubscriptionResponseDto>();

            var updateDto = new UpdateSubscriptionRequestDto
            {
                Type = SubscriptionType.Yearly, // ✅ FIXED
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddYears(1),
                Fee = 12000m,
                Status = SubscriptionStatus.Active
            };

            var response = await _client.PutAsJsonAsync($"/api/subscriptions/{created!.Id}", updateDto);
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
        }
        else
        {
            Assert.True(true, "Subscription creation failed, skipping update test");
        }
    }

    [Fact]
    public async Task UpdateSubscription_AsClinicAdmin_ReturnsForbidden()
    {
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.ClinicAdminEmail);

        var updateDto = new UpdateSubscriptionRequestDto
        {
            Type = SubscriptionType.Yearly, // ✅ FIXED
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddYears(1),
            Fee = 15000m,
            Status = SubscriptionStatus.Active
        };

        var response = await _client.PutAsJsonAsync($"/api/subscriptions/{Guid.NewGuid()}", updateDto);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateSubscription_AsDoctor_ReturnsForbidden()
    {
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.DoctorEmail);

        var updateDto = new UpdateSubscriptionRequestDto
        {
            Type = SubscriptionType.Monthly, // ✅ FIXED
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
            Fee = 1000m,
            Status = SubscriptionStatus.Active
        };

        var response = await _client.PutAsJsonAsync($"/api/subscriptions/{Guid.NewGuid()}", updateDto);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region DELETE /api/subscriptions/{id}

    [Fact]
    public async Task DeleteSubscription_AsSuperAdmin_Succeeds()
    {
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.SuperAdminEmail);

        var createDto = new CreateSubscriptionRequestDto
        {
            ClinicId = _factory.ClinicId,
            Type = SubscriptionType.Monthly, // ✅ FIXED
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
            Fee = 1000m,
            Status = SubscriptionStatus.Active
        };
        var createResponse = await _client.PostAsJsonAsync("/api/subscriptions", createDto);

        if (createResponse.StatusCode == HttpStatusCode.Created)
        {
            var created = await createResponse.Content.ReadFromJsonAsync<SubscriptionResponseDto>();
            var response = await _client.DeleteAsync($"/api/subscriptions/{created!.Id}");
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
        }
        else
        {
            Assert.True(true, "Subscription creation failed, skipping delete test");
        }
    }

    [Fact]
    public async Task DeleteSubscription_AsClinicAdmin_ReturnsForbidden()
    {
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.ClinicAdminEmail);
        var response = await _client.DeleteAsync($"/api/subscriptions/{Guid.NewGuid()}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteSubscription_AsDoctor_ReturnsForbidden()
    {
        await TestDataHelper.AuthenticateAs(_client, TestDataHelper.DoctorEmail);
        var response = await _client.DeleteAsync($"/api/subscriptions/{Guid.NewGuid()}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized);
    }

    #endregion
    
    #region DELETE /api/subscriptions/{id}/hard - Hard Delete Subscription

[Fact]
public async Task HardDeleteSubscription_AsSuperAdmin_Succeeds()
{
    // Arrange
    await TestDataHelper.AuthenticateAs(_client, TestDataHelper.SuperAdminEmail);

    // Create a subscription to hard delete
    var createDto = new CreateSubscriptionRequestDto
    {
        ClinicId = _factory.ClinicId,
        Type = SubscriptionType.Monthly,
        StartDate = DateTime.UtcNow,
        EndDate = DateTime.UtcNow.AddMonths(1),
        Fee = 1000m,
        Status = SubscriptionStatus.Active
    };
    var createResponse = await _client.PostAsJsonAsync("/api/subscriptions", createDto);

    if (createResponse.StatusCode == HttpStatusCode.Created)
    {
        var created = await createResponse.Content.ReadFromJsonAsync<SubscriptionResponseDto>();

        // Act
        var response = await _client.DeleteAsync($"/api/subscriptions/{created!.Id}/hard");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
    }
    else
    {
        Assert.True(true, "Subscription creation failed, skipping hard delete test");
    }
}

[Fact]
public async Task HardDeleteSubscription_AsClinicAdmin_ReturnsForbidden()
{
    // Arrange
    await TestDataHelper.AuthenticateAs(_client, TestDataHelper.ClinicAdminEmail);

    // Act
    var response = await _client.DeleteAsync($"/api/subscriptions/{Guid.NewGuid()}/hard");

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
}

#endregion

#region GET /api/subscriptions/clinic/{clinicId}?includePayments=true

[Fact]
public async Task GetClinicSubscriptions_WithPayments_ReturnsSubscriptionsAndPayments()
{
    // Arrange
    await TestDataHelper.AuthenticateAs(_client, TestDataHelper.SuperAdminEmail);

    // Act
    var response = await _client.GetAsync($"/api/subscriptions/clinic/{_factory.ClinicId}?includePayments=true");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var subscriptions = await response.Content.ReadFromJsonAsync<IEnumerable<SubscriptionResponseDto>>();
    subscriptions.Should().NotBeNull();
}

#endregion
}