using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Fdw.Services.Authorization.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Clients.Tests;

public sealed class RoleApiClientTests
{
    private static RoleApiClient CreateClient(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.example.com/") };
        return new RoleApiClient(httpClient, Mock.Of<ILogger<RoleApiClient>>());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task GetRolesSendsGetRequestToCorrectPath()
    {
        var expected = new List<RoleSummaryPayload>
        {
            new() { Id = Guid.NewGuid(), Name = "Admin" }
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create<IReadOnlyList<RoleSummaryPayload>>(expected)
        });

        var sut = CreateClient(handler);
        var result = await sut.GetRoles(TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/roles");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Count.ShouldBe(1);
        result.Value[0].Name.ShouldBe("Admin");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task GetRolesReturnsFailureOnError()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));

        var sut = CreateClient(handler);
        var result = await sut.GetRoles(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task GetRoleSendsGetRequestWithNameInPath()
    {
        var expected = new RoleDetailPayload { Id = Guid.NewGuid(), Name = "Admin" };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });

        var sut = CreateClient(handler);
        var result = await sut.GetRole("Admin", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/roles/Admin");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("Admin");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task GetRoleReturnsFailureOnError()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));

        var sut = CreateClient(handler);
        var result = await sut.GetRole("Admin", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task CreateRoleSendsPostRequestToCorrectPath()
    {
        var expected = new RoleDetailPayload { Id = Guid.NewGuid(), Name = "Editor" };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);
        var request = new CreateRolePayload { Name = "Editor", DisplayName = "Editor Role" };

        var result = await sut.CreateRole(request, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/roles");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("Editor");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task CreateRoleReturnsFailureOnFailureStatusCode()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Conflict));
        var sut = CreateClient(handler);
        var request = new CreateRolePayload { Name = "Editor" };

        var result = await sut.CreateRole(request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task CreateRoleReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var sut = CreateClient(handler);
        var request = new CreateRolePayload { Name = "Editor" };

        var result = await sut.CreateRole(request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task UpdateRoleSendsPutRequestWithNameInPath()
    {
        var expected = new RoleDetailPayload { Id = Guid.NewGuid(), Name = "Editor", DisplayName = "Updated Editor" };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);
        var request = new UpdateRolePayload { DisplayName = "Updated Editor" };

        var result = await sut.UpdateRole("Editor", request, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/roles/Editor");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Put);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.DisplayName.ShouldBe("Updated Editor");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task UpdateRoleReturnsFailureOnFailureStatusCode()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = CreateClient(handler);
        var request = new UpdateRolePayload { DisplayName = "Updated Editor" };

        var result = await sut.UpdateRole("Editor", request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task UpdateRoleReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var sut = CreateClient(handler);
        var request = new UpdateRolePayload { DisplayName = "Updated Editor" };

        var result = await sut.UpdateRole("Editor", request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task DeleteRoleSendsDeleteRequestWithNameInPath()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateClient(handler);

        var result = await sut.DeleteRole("Editor", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/roles/Editor");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Delete);
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task DeleteRoleReturnsFailureOnFailureStatusCode()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = CreateClient(handler);

        var result = await sut.DeleteRole("Editor", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task DeleteRoleReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var sut = CreateClient(handler);

        var result = await sut.DeleteRole("Editor", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task GetPermissionsSendsGetRequestToCorrectPath()
    {
        var expected = new List<PermissionPayload>
        {
            new() { Id = Guid.NewGuid(), Name = "fdw:connections:read", Resource = "connections", Action = "read" }
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create<IReadOnlyList<PermissionPayload>>(expected)
        });

        var sut = CreateClient(handler);
        var result = await sut.GetPermissions(TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/permissions");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task GetPermissionsReturnsFailureOnError()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));

        var sut = CreateClient(handler);
        var result = await sut.GetPermissions(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task GetPermissionsGroupedSendsGetRequestToCorrectPath()
    {
        var expected = new List<PermissionGroupPayload>
        {
            new()
            {
                Resource = "connections",
                Permissions = [new PermissionPayload { Name = "fdw:connections:read", Resource = "connections", Action = "read" }]
            }
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create<IReadOnlyList<PermissionGroupPayload>>(expected)
        });

        var sut = CreateClient(handler);
        var result = await sut.GetPermissionsGrouped(TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/permissions/grouped");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Count.ShouldBe(1);
        result.Value[0].Resource.ShouldBe("connections");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task GetPermissionsGroupedReturnsFailureOnError()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));

        var sut = CreateClient(handler);
        var result = await sut.GetPermissionsGrouped(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task GetRolePermissionsSendsGetRequestWithNameInPath()
    {
        var expected = new List<PermissionPayload>
        {
            new() { Id = Guid.NewGuid(), Name = "fdw:connections:read", Resource = "connections", Action = "read" }
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create<IReadOnlyList<PermissionPayload>>(expected)
        });

        var sut = CreateClient(handler);
        var result = await sut.GetRolePermissions("Admin", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/roles/Admin/permissions");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task GetRolePermissionsReturnsFailureOnError()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));

        var sut = CreateClient(handler);
        var result = await sut.GetRolePermissions("Admin", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task SetRolePermissionsSendsPutRequestWithNameInPath()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateClient(handler);
        var request = new SetRolePermissionsPayload { PermissionNames = ["fdw:connections:read", "fdw:connections:write"] };

        var result = await sut.SetRolePermissions("Admin", request, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/roles/Admin/permissions");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Put);
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task SetRolePermissionsReturnsFailureOnFailureStatusCode()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest));
        var sut = CreateClient(handler);
        var request = new SetRolePermissionsPayload { PermissionNames = ["fdw:connections:read"] };

        var result = await sut.SetRolePermissions("Admin", request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task SetRolePermissionsReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var sut = CreateClient(handler);
        var request = new SetRolePermissionsPayload { PermissionNames = ["fdw:connections:read"] };

        var result = await sut.SetRolePermissions("Admin", request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task GetRoleWithSpecialCharactersInNameEncodesPath()
    {
        var expected = new RoleDetailPayload { Name = "my role" };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        await sut.GetRole("my role", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldContain("roles/my%20role");
    }
}
