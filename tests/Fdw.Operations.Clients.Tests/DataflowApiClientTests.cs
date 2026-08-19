using System.Net;
using System.Net.Http.Json;
using Fdw.Operations.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Clients.Tests;

public sealed class DataflowApiClientTests
{
    private static DataflowApiClient CreateClient(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.example.com/") };
        return new DataflowApiClient(httpClient, Mock.Of<ILogger<DataflowApiClient>>());
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetGraphSendsCorrectRequest()
    {
        var expected = new DataflowGraphPayload
        {
            Nodes = [new DataflowNodeResponse()],
            Edges = [new DataflowEdgeResponse()],
            Stats = new DataflowStatsResponse()
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.GetGraph(TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/dataflow/graph");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Nodes.Count.ShouldBe(1);
        result.Value.Edges.Count.ShouldBe(1);
        result.Value.Stats.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetLineageSendsCorrectRequest()
    {
        var expected = new DataSetLineagePayload
        {
            DataSetName = "TestDataSet"
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.GetLineage("TestDataSet", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/dataflow/lineage/TestDataSet");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.DataSetName.ShouldBe("TestDataSet");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetLineageEscapesDatasetName()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new DataSetLineagePayload { DataSetName = "My DataSet" })
        });
        var sut = CreateClient(handler);

        await sut.GetLineage("My DataSet", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/dataflow/lineage/My%20DataSet");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task AnalyzeImpactSendsCorrectRequest()
    {
        var expected = new ImpactAnalysisPayload
        {
            TargetType = "Connection",
            TargetName = "MyConn",
            TotalImpactedCount = 3,
            HighImpactCount = 1
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.AnalyzeImpact("Connection", "MyConn", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/dataflow/impact");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.TargetType.ShouldBe("Connection");
        result.Value.TargetName.ShouldBe("MyConn");
        result.Value.TotalImpactedCount.ShouldBe(3);
        result.Value.HighImpactCount.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task AnalyzeImpactCarriesTheTargetInTheBody()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new ImpactAnalysisPayload())
        });
        var sut = CreateClient(handler);

        await sut.AnalyzeImpact("Data Store", "My Conn", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/dataflow/impact");
        // Why the values are asserted unescaped: they travel in the body now, so a target with a
        // space is carried as written rather than percent-encoded into a path segment.
        var body = await handler.LastRequest.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldContain("Data Store");
        body.ShouldContain("My Conn");
    }
}
