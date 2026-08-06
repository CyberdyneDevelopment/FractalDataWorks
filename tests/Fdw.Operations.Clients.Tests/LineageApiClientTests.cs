using System.Net;
using System.Net.Http.Json;
using Fdw.Operations.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Clients.Tests;

public sealed class LineageApiClientTests
{
    private static LineageApiClient CreateClient(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.example.com/") };
        return new LineageApiClient(httpClient, Mock.Of<ILogger<LineageApiClient>>());
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetLineageSendsCorrectRequest()
    {
        var expected = new LineageGraphPayload
        {
            Nodes = [new LineageNodePayload()],
            Edges = [new LineageEdgePayload()]
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.GetLineage("DataSet", "MyDataSet", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/lineage/DataSet/MyDataSet");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Nodes.Count.ShouldBe(1);
        result.Value.Edges.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetColumnLineageSendsCorrectRequest()
    {
        var expected = new LineageGraphPayload
        {
            Nodes = [],
            Edges = []
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.GetColumnLineage("DataStore", "MyStore", "FieldA", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/lineage/DataStore/MyStore/fields/FieldA");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
    }
}
