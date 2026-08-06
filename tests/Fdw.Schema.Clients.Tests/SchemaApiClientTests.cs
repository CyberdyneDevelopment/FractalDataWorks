using System.Net;
using System.Net.Http.Json;
using Fdw.Schema.Clients.Models;
using Fdw.Services.Data.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Schema.Clients.Tests;

public sealed class SchemaApiClientTests
{
    private static SchemaApiClient CreateClient(MockHttpMessageHandler handler, string? connectionName = null)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.example.com/") };
        var client = new SchemaApiClient(httpClient, Mock.Of<ILogger<SchemaApiClient>>());
        if (connectionName is not null)
        {
            client.SetConnection(connectionName);
        }

        return client;
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task DiscoverSchemaSendsCorrectRequest()
    {
        var expected = new SchemaDiscoveryResponse
        {
            ConnectionName = "MyConn",
            ConnectionType = "MsSql",
            DatabaseName = "TestDb",
            TotalTableCount = 5,
            TotalViewCount = 2
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler, "MyConn");

        var result = await sut.DiscoverSchema(TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/connections/MyConn/schema");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.ConnectionName.ShouldBe("MyConn");
        result.Value.TotalTableCount.ShouldBe(5);
        result.Value.TotalViewCount.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task DiscoverSchemaEscapesConnectionName()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new SchemaDiscoveryResponse { ConnectionName = "My Conn" })
        });
        var sut = CreateClient(handler, "My Conn");

        await sut.DiscoverSchema(TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/connections/My%20Conn/schema");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetCapableConnectionsSendsCorrectRequest()
    {
        var expected = new List<SchemaCapableConnectionPayload>
        {
            new() { Name = "Conn1", ConnectionType = "MsSql", IsAvailable = true }
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.GetCapableConnections(TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/connections/schema-capable");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Count.ShouldBe(1);
        result.Value[0].Name.ShouldBe("Conn1");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task PreviewDataSendsCorrectRequest()
    {
        var request = new SchemaPreviewRequest
        {
            DataSetName = "TestDs",
            DataStoreName = "MyStore",
            PathName = "dbo",
            ContainerName = "Users",
            MaxRows = 50
        };
        var expected = new DataPreviewResponsePayload
        {
            Columns = [new ColumnSchemaPayload { Name = "Id", DataType = "int" }],
            HasMoreRows = true,
            TotalRowCount = 100
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.PreviewData(request, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/schema/preview");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Columns.Count.ShouldBe(1);
        result.Value.HasMoreRows.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task SyncSchemaSendsCorrectRequest()
    {
        var expected = new SyncSchemaResponse
        {
            DataStoreName = "MyConn",
            HasChanges = true,
            AddedTables = ["NewTable"]
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler, "MyConn");

        var result = await sut.SyncSchema(applyChanges: false, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/connections/MyConn/sync-schema");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.HasChanges.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task ImportSchemaSendsCorrectRequest()
    {
        var request = new ImportSchemaRequestPayload { DataStoreName = "TestStore" };
        var expected = new ImportSchemaResponse
        {
            Success = true,
            DataStoreName = "TestStore",
            TablesImported = 3,
            ColumnsImported = 12
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler, "MyConn");

        var result = await sut.ImportSchema(request, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/connections/MyConn/import-schema");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.TablesImported.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void SetConnectionSetsCurrentConnection()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateClient(handler);

        sut.CurrentConnection.ShouldBeNull();
        sut.SetConnection("TestConn");
        sut.CurrentConnection.ShouldBe("TestConn");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void SetConnectionThrowsOnEmptyName()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateClient(handler);

        Should.Throw<ArgumentException>(() => sut.SetConnection(""));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task DiscoverSchemaThrowsWithoutConnection()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateClient(handler);

        await Should.ThrowAsync<InvalidOperationException>(
            () => sut.DiscoverSchema(TestContext.Current.CancellationToken));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task ExecuteDdlSendsCorrectRequest()
    {
        var expected = new ExecuteDdlResponse { Success = true, Message = "OK" };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler, "MyConn");

        var result = await sut.ExecuteDdl("CREATE TABLE Test (Id INT)", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/connections/MyConn/execute-ddl");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Success.ShouldBeTrue();
    }
}
