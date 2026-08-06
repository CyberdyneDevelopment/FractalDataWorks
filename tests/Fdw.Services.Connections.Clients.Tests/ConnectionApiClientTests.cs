using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Fdw.Services.Connections.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Clients.Tests;

public sealed class ConnectionApiClientTests
{
    private static readonly Uri BaseUri = new("https://test.example.com/");

    private static ConnectionApiClient CreateClient(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = BaseUri };
        return new ConnectionApiClient(httpClient, Mock.Of<ILogger<ConnectionApiClient>>());
    }

    // --- GetConnections ---

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetConnectionsSendsCorrectRequest()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<ConnectionPayload>())
        });
        var sut = CreateClient(handler);

        await sut.GetConnections(TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/connections");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetConnectionsReturnsDeserializedResponse()
    {
        var connections = new List<ConnectionPayload>
        {
            new() { Name = "conn1", ConnectionType = "MsSql" }
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(connections)
        });
        var sut = CreateClient(handler);

        var result = await sut.GetConnections(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Count.ShouldBe(1);
        result.Value[0].Name.ShouldBe("conn1");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetConnectionsReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var sut = CreateClient(handler);

        var result = await sut.GetConnections(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    // --- GetConnectionTypes ---

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetConnectionTypesSendsCorrectRequest()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<ConnectionTypePayload>())
        });
        var sut = CreateClient(handler);

        await sut.GetConnectionTypes(TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/connections/types");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetConnectionTypesReturnsDeserializedResponse()
    {
        var types = new List<ConnectionTypePayload>
        {
            new() { Id = "mssql", Name = "MsSql", Category = "Database" }
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(types)
        });
        var sut = CreateClient(handler);

        var result = await sut.GetConnectionTypes(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Count.ShouldBe(1);
        result.Value[0].Name.ShouldBe("MsSql");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetConnectionTypesReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var sut = CreateClient(handler);

        var result = await sut.GetConnectionTypes(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    // --- GetConnection ---

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetConnectionSendsCorrectRequest()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new ConnectionDetailResponse { Name = "myconn" })
        });
        var sut = CreateClient(handler);

        await sut.GetConnection("myconn", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/connections/myconn");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetConnectionReturnsDeserializedResponse()
    {
        var detail = new ConnectionDetailResponse { Name = "myconn", Server = "localhost", Port = 1433 };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(detail)
        });
        var sut = CreateClient(handler);

        var result = await sut.GetConnection("myconn", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("myconn");
        result.Value.Server.ShouldBe("localhost");
        result.Value.Port.ShouldBe(1433);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetConnectionReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var sut = CreateClient(handler);

        var result = await sut.GetConnection("myconn", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    // --- CreateConnection ---

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task CreateConnectionSendsCorrectRequest()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new ConnectionDetailResponse())
        });
        var sut = CreateClient(handler);

        await sut.CreateConnection(new CreateConnectionClientRequest { Name = "new" }, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/connections");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
    }

    [Theory]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    [InlineData("Http", "/connections/http")]
    [InlineData("PostgreSql", "/connections/postgresql")]
    [InlineData("FileSystem", "/connections/filesystem")]
    [InlineData("RoslynWorkspace", "/connections/roslynworkspace")]
    [InlineData("MsSql", "/connections")]
    public async Task CreateConnectionDispatchesToPerTypeRoute(string serviceType, string expectedPath)
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new ConnectionDetailResponse())
        });
        var sut = CreateClient(handler);

        await sut.CreateConnection(
            new CreateConnectionClientRequest { Name = "new", ServiceType = serviceType },
            TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe(expectedPath);
        handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task CreateConnectionReturnsDeserializedResponseOnSuccess()
    {
        var detail = new ConnectionDetailResponse { Name = "new", ServiceType = "MsSql" };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(detail)
        });
        var sut = CreateClient(handler);

        var result = await sut.CreateConnection(new CreateConnectionClientRequest { Name = "new" }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("new");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task CreateConnectionReturnsFailureOnNonSuccessStatus()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest));
        var sut = CreateClient(handler);

        var result = await sut.CreateConnection(new CreateConnectionClientRequest(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task CreateConnectionReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var sut = CreateClient(handler);

        var result = await sut.CreateConnection(new CreateConnectionClientRequest(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    // --- UpdateConnection ---

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task UpdateConnectionSendsCorrectRequest()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new ConnectionDetailResponse())
        });
        var sut = CreateClient(handler);

        await sut.UpdateConnection("myconn", new UpdateConnectionClientRequest { Server = "newhost" }, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/connections/myconn");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Put);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task UpdateConnectionReturnsDeserializedResponseOnSuccess()
    {
        var detail = new ConnectionDetailResponse { Name = "myconn", Server = "newhost" };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(detail)
        });
        var sut = CreateClient(handler);

        var result = await sut.UpdateConnection("myconn", new UpdateConnectionClientRequest(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Server.ShouldBe("newhost");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task UpdateConnectionReturnsFailureOnNonSuccessStatus()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = CreateClient(handler);

        var result = await sut.UpdateConnection("myconn", new UpdateConnectionClientRequest(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task UpdateConnectionReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var sut = CreateClient(handler);

        var result = await sut.UpdateConnection("myconn", new UpdateConnectionClientRequest(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    // --- DeleteConnection ---

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task DeleteConnectionSendsCorrectRequest()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateClient(handler);

        await sut.DeleteConnection("myconn", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/connections/myconn");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Delete);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task DeleteConnectionReturnsSuccessOnSuccess()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateClient(handler);

        var result = await sut.DeleteConnection("myconn", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task DeleteConnectionReturnsFailureOnNonSuccessStatus()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = CreateClient(handler);

        var result = await sut.DeleteConnection("myconn", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task DeleteConnectionReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var sut = CreateClient(handler);

        var result = await sut.DeleteConnection("myconn", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    // --- TestConnection ---

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task TestConnectionSendsCorrectRequest()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new TestConnectionClientResponse())
        });
        var sut = CreateClient(handler);

        await sut.TestConnection("myconn", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/connections/myconn/test");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task TestConnectionReturnsDeserializedResponseOnSuccess()
    {
        var response = new TestConnectionClientResponse { Name = "myconn", Success = true, Message = "OK" };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(response)
        });
        var sut = CreateClient(handler);

        var result = await sut.TestConnection("myconn", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Success.ShouldBeTrue();
        result.Value.Message.ShouldBe("OK");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task TestConnectionReturnsFailureOnNonSuccessStatus()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var sut = CreateClient(handler);

        var result = await sut.TestConnection("myconn", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task TestConnectionReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var sut = CreateClient(handler);

        var result = await sut.TestConnection("myconn", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }
}
