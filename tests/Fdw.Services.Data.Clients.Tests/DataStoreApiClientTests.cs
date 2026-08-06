using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Fdw.Services.Data.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Clients.Tests;

public sealed class DataStoreApiClientTests
{
    private static readonly Uri BaseUri = new("https://test.example.com/");

    private static DataStoreApiClient CreateClient(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = BaseUri };
        return new DataStoreApiClient(httpClient, Mock.Of<ILogger<DataStoreApiClient>>());
    }

    // --- GetDataStores ---

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetDataStoresSendsCorrectRequest()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<DataStoreSummaryPayload>())
        });
        var sut = CreateClient(handler);

        await sut.GetDataStores(TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/datastores");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetDataStoresReturnsDeserializedResponse()
    {
        var stores = new List<DataStoreSummaryPayload>
        {
            new() { Name = "store1", ConnectionName = "conn1" }
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(stores)
        });
        var sut = CreateClient(handler);

        var result = await sut.GetDataStores(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Count.ShouldBe(1);
        result.Value[0].Name.ShouldBe("store1");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetDataStoresReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var sut = CreateClient(handler);

        var result = await sut.GetDataStores(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    // --- GetDataStore ---

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetDataStoreSendsCorrectRequest()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new DataStoreDetailPayload { Name = "store1" })
        });
        var sut = CreateClient(handler);

        await sut.GetDataStore("store1", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/datastores/store1");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetDataStoreReturnsDeserializedResponse()
    {
        var detail = new DataStoreDetailPayload { Name = "store1", ConnectionName = "conn1", StoreType = "SqlServer" };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(detail)
        });
        var sut = CreateClient(handler);

        var result = await sut.GetDataStore("store1", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("store1");
        result.Value.StoreType.ShouldBe("SqlServer");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetDataStoreReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var sut = CreateClient(handler);

        var result = await sut.GetDataStore("store1", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    // --- CreateDataStore ---

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task CreateDataStoreSendsCorrectRequest()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new DataStoreDetailPayload())
        });
        var sut = CreateClient(handler);

        await sut.CreateDataStore(new CreateDataStoreWithPathsRequest { Name = "new" }, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/datastores");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task CreateDataStoreReturnsDeserializedResponseOnSuccess()
    {
        var detail = new DataStoreDetailPayload { Name = "new", StoreType = "SqlServer" };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(detail)
        });
        var sut = CreateClient(handler);

        var result = await sut.CreateDataStore(new CreateDataStoreWithPathsRequest { Name = "new" }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("new");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task CreateDataStoreReturnsFailureOnNonSuccessStatus()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest));
        var sut = CreateClient(handler);

        var result = await sut.CreateDataStore(new CreateDataStoreWithPathsRequest(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task CreateDataStoreReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var sut = CreateClient(handler);

        var result = await sut.CreateDataStore(new CreateDataStoreWithPathsRequest(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    // --- UpdateDataStore ---

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task UpdateDataStoreSendsCorrectRequest()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new DataStoreDetailPayload())
        });
        var sut = CreateClient(handler);

        await sut.UpdateDataStore("store1", new UpdateDataStoreWithPathsRequest(), TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/datastores/store1");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Put);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task UpdateDataStoreReturnsDeserializedResponseOnSuccess()
    {
        var detail = new DataStoreDetailPayload { Name = "store1", ConnectionName = "updated" };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(detail)
        });
        var sut = CreateClient(handler);

        var result = await sut.UpdateDataStore("store1", new UpdateDataStoreWithPathsRequest(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.ConnectionName.ShouldBe("updated");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task UpdateDataStoreReturnsFailureOnNonSuccessStatus()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = CreateClient(handler);

        var result = await sut.UpdateDataStore("store1", new UpdateDataStoreWithPathsRequest(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task UpdateDataStoreReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var sut = CreateClient(handler);

        var result = await sut.UpdateDataStore("store1", new UpdateDataStoreWithPathsRequest(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    // --- DeleteDataStore ---

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task DeleteDataStoreSendsCorrectRequest()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateClient(handler);

        await sut.DeleteDataStore("store1", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/datastores/store1");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Delete);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task DeleteDataStoreReturnsSuccessOnSuccess()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateClient(handler);

        var result = await sut.DeleteDataStore("store1", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task DeleteDataStoreReturnsFailureOnNonSuccessStatus()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = CreateClient(handler);

        var result = await sut.DeleteDataStore("store1", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task DeleteDataStoreReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var sut = CreateClient(handler);

        var result = await sut.DeleteDataStore("store1", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    // --- DiscoverContainers ---

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task DiscoverContainersSendsCorrectRequest()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new DiscoveryResultPayload())
        });
        var sut = CreateClient(handler);

        await sut.DiscoverContainers(new DiscoverDataStoreRequest { Name = "store1" }, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        // Why: aligned to the server's POST DiscoverDataStoreEndpointBase; the bare
        // "/datastores/discover" is shadowed by the "/datastores/{name}" catch-all, so both the
        // client and server use the non-colliding "/datastores/-/discover" form.
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/datastores/-/discover");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task DiscoverContainersSendsRequestBody()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new DiscoveryResultPayload())
        });
        var sut = CreateClient(handler);

        await sut.DiscoverContainers(
            new DiscoverDataStoreRequest { Name = "store1", Refresh = true },
            TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.Content.ShouldNotBeNull();
        var body = await handler.LastRequest.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldContain("\"name\":\"store1\"");
        body.ShouldContain("\"refresh\":true");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task DiscoverContainersReturnsDeserializedResponse()
    {
        var summary = new DiscoveryResultPayload
        {
            DataStoreName = "store1",
            ContainerCount = 3,
            FieldCount = 12
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(summary)
        });
        var sut = CreateClient(handler);

        var result = await sut.DiscoverContainers(new DiscoverDataStoreRequest { Name = "store1" }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.DataStoreName.ShouldBe("store1");
        result.Value.ContainerCount.ShouldBe(3);
        result.Value.FieldCount.ShouldBe(12);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task DiscoverContainersReturnsFailureOnNonSuccessStatus()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var sut = CreateClient(handler);

        var result = await sut.DiscoverContainers(new DiscoverDataStoreRequest { Name = "store1" }, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task DiscoverContainersReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var sut = CreateClient(handler);

        var result = await sut.DiscoverContainers(new DiscoverDataStoreRequest { Name = "store1" }, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }
}
