using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Fdw.Services.Multitenancy.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Clients.Tests;

public sealed class TenantApiClientTests
{
    private static readonly Guid TestTenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static TenantApiClient CreateClient(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.example.com/") };
        return new TenantApiClient(httpClient, Mock.Of<ILogger<TenantApiClient>>());
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetTenantsSendsGetRequestToCorrectPathWithDefaultIncludeInactive()
    {
        var expected = new List<TenantSummaryPayload>
        {
            new() { Id = TestTenantId, Name = "Acme Corp", Slug = "acme", IsActive = true }
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create<IReadOnlyList<TenantSummaryPayload>>(expected)
        });

        var sut = CreateClient(handler);
        var result = await sut.GetTenants(ct: TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/tenants?includeInactive=False");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Count.ShouldBe(1);
        result.Value[0].Name.ShouldBe("Acme Corp");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetTenantsSendsGetRequestWithIncludeInactiveTrue()
    {
        var expected = new List<TenantSummaryPayload>
        {
            new() { Id = TestTenantId, Name = "Acme Corp", Slug = "acme", IsActive = true },
            new() { Id = Guid.NewGuid(), Name = "Inactive Corp", Slug = "inactive", IsActive = false }
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create<IReadOnlyList<TenantSummaryPayload>>(expected)
        });

        var sut = CreateClient(handler);
        var result = await sut.GetTenants(includeInactive: true, ct: TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/tenants?includeInactive=True");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetTenantsReturnsFailureOnError()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));

        var sut = CreateClient(handler);
        var result = await sut.GetTenants(ct: TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetTenantSendsGetRequestWithIdInPath()
    {
        var expected = new TenantDetailPayload { Id = TestTenantId, Name = "Acme Corp", Slug = "acme", IsActive = true };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });

        var sut = CreateClient(handler);
        var result = await sut.GetTenant(TestTenantId, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe($"/tenants/{TestTenantId}");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("Acme Corp");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetTenantReturnsFailureOnError()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));

        var sut = CreateClient(handler);
        var result = await sut.GetTenant(TestTenantId, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetCurrentTenantSendsGetRequestToCorrectPath()
    {
        var expected = new TenantDetailPayload { Id = TestTenantId, Name = "Current Tenant", Slug = "current", IsActive = true };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });

        var sut = CreateClient(handler);
        var result = await sut.GetCurrentTenant(TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/tenants/current");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("Current Tenant");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetCurrentTenantReturnsFailureOnError()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));

        var sut = CreateClient(handler);
        var result = await sut.GetCurrentTenant(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task CreateTenantSendsPostRequestToCorrectPath()
    {
        var expected = new TenantDetailPayload { Id = TestTenantId, Name = "New Tenant", Slug = "new-tenant", IsActive = true };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);
        var request = new CreateTenantRequest { Name = "New Tenant", Slug = "new-tenant" };

        var result = await sut.CreateTenant(request, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/tenants");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("New Tenant");
        result.Value!.Slug.ShouldBe("new-tenant");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task CreateTenantReturnsFailureOnFailureStatusCode()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Conflict));
        var sut = CreateClient(handler);
        var request = new CreateTenantRequest { Name = "New Tenant", Slug = "new-tenant" };

        var result = await sut.CreateTenant(request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task CreateTenantReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var sut = CreateClient(handler);
        var request = new CreateTenantRequest { Name = "New Tenant", Slug = "new-tenant" };

        var result = await sut.CreateTenant(request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task UpdateTenantSendsPatchRequestWithIdInPath()
    {
        var expected = new TenantDetailPayload { Id = TestTenantId, Name = "Updated Tenant", Slug = "acme", IsActive = true };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);
        var request = new UpdateTenantRequest { Name = "Updated Tenant" };

        var result = await sut.UpdateTenant(TestTenantId, request, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe($"/tenants/{TestTenantId}");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Patch);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("Updated Tenant");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task UpdateTenantReturnsFailureOnFailureStatusCode()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = CreateClient(handler);
        var request = new UpdateTenantRequest { Name = "Updated Tenant" };

        var result = await sut.UpdateTenant(TestTenantId, request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task UpdateTenantReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var sut = CreateClient(handler);
        var request = new UpdateTenantRequest { Name = "Updated Tenant" };

        var result = await sut.UpdateTenant(TestTenantId, request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task SwitchTenantSendsPostRequestToCorrectPath()
    {
        var expected = new SwitchTenantResponse { Success = true, AccessToken = "new-token", ExpiresIn = 3600 };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);
        var request = new SwitchTenantRequest { TenantId = TestTenantId };

        var result = await sut.SwitchTenant(request, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/tenants/switch");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Success.ShouldBeTrue();
        result.Value!.AccessToken.ShouldBe("new-token");
        result.Value!.ExpiresIn.ShouldBe(3600);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task SwitchTenantReturnsFailureOnFailureStatusCode()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Forbidden));
        var sut = CreateClient(handler);
        var request = new SwitchTenantRequest { TenantId = TestTenantId };

        var result = await sut.SwitchTenant(request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task SwitchTenantReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var sut = CreateClient(handler);
        var request = new SwitchTenantRequest { TenantId = TestTenantId };

        var result = await sut.SwitchTenant(request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }
}
