using System.Net;
using System.Net.Http.Json;
using Fdw.Schema.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Schema.Clients.Tests;

public sealed class TableApiClientTests
{
    private static TableApiClient CreateClient(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.example.com/") };
        return new TableApiClient(httpClient, Mock.Of<ILogger<TableApiClient>>());
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GenerateDdlSendsCorrectRequest()
    {
        var request = new CreateTableRequest
        {
            ConnectionName = "MyConn",
            SchemaName = "dbo",
            TableName = "NewTable",
            Columns =
            [
                new TableColumnRequest { Name = "Name", DataType = "String", MaxLength = 200, IsRequired = true }
            ]
        };
        var expected = new DdlResponse { Ddl = "CREATE TABLE [dbo].[NewTable] ..." };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.GenerateDdl(request, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/connections/MyConn/generate-ddl");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Ddl.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task ExecuteDdlSendsCorrectRequest()
    {
        var request = new ExecuteDdlRequestPayload
        {
            ConnectionName = "MyConn",
            Ddl = "CREATE TABLE [dbo].[Test] (Id INT PRIMARY KEY)"
        };
        var expected = new ExecuteDdlResponse { Success = true, Message = "Executed successfully" };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.ExecuteDdl(request, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/connections/MyConn/execute-ddl");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Success.ShouldBeTrue();
        result.Value.Message.ShouldBe("Executed successfully");
    }
}
