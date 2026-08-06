using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Clients.Tests;

public sealed class PipelineHttpClientTests
{
    private static readonly Uri BaseUri = new("https://test.example.com/");

    private static PipelineHttpClient CreateClient(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = BaseUri };
        return new PipelineHttpClient(httpClient, Mock.Of<ILogger<PipelineHttpClient>>());
    }

    // --- List ---

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task ListSendsGetRequestToCorrectPath()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<PipelineSummaryResponse>())
        });
        var sut = CreateClient(handler);

        await sut.List(TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/pipelines");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task ListReturnsDeserializedList()
    {
        var pipelines = new List<PipelineSummaryResponse>
        {
            new() { Name = "demo-pipeline", PipelineType = "BatchCopy" },
            new() { Name = "nfl-pipeline", PipelineType = "Streaming" }
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(pipelines)
        });
        var sut = CreateClient(handler);

        var result = await sut.List(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Count.ShouldBe(2);
        result.Value[0].Name.ShouldBe("demo-pipeline");
        result.Value[0].PipelineType.ShouldBe("BatchCopy");
        result.Value[1].Name.ShouldBe("nfl-pipeline");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task ListReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var sut = CreateClient(handler);

        var result = await sut.List(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    // --- Get ---

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetSendsGetRequestToCorrectPath()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new PipelineDetailResponse())
        });
        var sut = CreateClient(handler);

        await sut.Get("demo-pipeline", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/pipelines/demo-pipeline");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetReturnsDeserializedResponse()
    {
        var detail = new PipelineDetailResponse
        {
            Id = Guid.NewGuid(),
            Name = "demo-pipeline",
            PipelineType = "BatchCopy",
            SourceConnectionName = "source-db",
            DestinationConnectionName = "target-db",
            IsEnabled = true
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(detail)
        });
        var sut = CreateClient(handler);

        var result = await sut.Get("demo-pipeline", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("demo-pipeline");
        result.Value.PipelineType.ShouldBe("BatchCopy");
        result.Value.SourceConnectionName.ShouldBe("source-db");
        result.Value.DestinationConnectionName.ShouldBe("target-db");
        result.Value.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var sut = CreateClient(handler);

        var result = await sut.Get("test", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }
}
