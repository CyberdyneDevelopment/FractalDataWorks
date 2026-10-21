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
            new() { Name = "demo-pipeline", PipelineType = "Graph" },
            new() { Name = "nfl-pipeline", PipelineType = "Graph" }
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
        result.Value[0].PipelineType.ShouldBe("Graph");
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
    public async Task GetSendsPostRequestWithNameInBody()
    {
        string? body = null;
        var handler = new MockHttpMessageHandler(async (request, ct) =>
        {
            body = await request.Content!.ReadAsStringAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new PipelineDetailResponse { Name = "demo-pipeline", PipelineType = "Graph" }) };
        });
        var sut = CreateClient(handler);

        await sut.Get("demo-pipeline", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/pipelines/get");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
        body.ShouldBe("{\"name\":\"demo-pipeline\"}");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetByIdSendsPostRequestWithOnlyTheIdInBody()
    {
        string? body = null;
        var id = new Guid("33333333-3333-3333-3333-333333333333");
        var handler = new MockHttpMessageHandler(async (request, ct) =>
        {
            body = await request.Content!.ReadAsStringAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new PipelineDetailResponse { Name = "demo-pipeline", PipelineType = "Graph" }) };
        });
        var sut = CreateClient(handler);

        await sut.Get(id, TestContext.Current.CancellationToken);

        handler.LastRequest!.RequestUri!.PathAndQuery.ShouldBe("/pipelines/get");
        body.ShouldBe("{\"id\":\"33333333-3333-3333-3333-333333333333\"}");
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
            PipelineType = "Graph",
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
        result.Value.PipelineType.ShouldBe("Graph");
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

    // --- A refused request shows the upstream's reason, and only that (FDW-1046) ---

    private static async Task<Fdw.Results.IGenericResult<PipelineDetailResponse>> GetAnswering(HttpResponseMessage answer)
        => await CreateClient(new MockHttpMessageHandler(answer)).Get("test", TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AnUpstreamProblemDetailIsTheReasonTheCallerMayBeShown()
    {
        var result = await GetAnswering(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                "{\"title\":\"The request was invalid\",\"detail\":\"Pipeline step 'W' is invalid: Unknown transform type 'Median'\",\"code\":\"ETL-20001\"}"),
        });

        result.IsFailure.ShouldBeTrue();
        result.Code!.FormatClientMessage(result.Details).ShouldBe("Pipeline step 'W' is invalid: Unknown transform type 'Median'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task TheReasonNeverHoldsTheUpstreamAddress()
    {
        var result = await GetAnswering(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"detail\":\"a reason\"}"),
        });

        var shown = result.Code!.FormatClientMessage(result.Details).ShouldNotBeNull();

        shown.ShouldBe("a reason");
        shown.ShouldNotContain("test.example.com");
        shown.ShouldNotContain("400");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    [InlineData("<html><body>Bad Request</body></html>")]
    [InlineData("plain text from a proxy")]
    [InlineData("{\"title\":\"only a title\"}")]
    [InlineData("{\"detail\":42}")]
    [InlineData("")]
    public async Task AnUpstreamAnswerThatIsNotAProblemBodyWithADetailGivesNoReason(string body)
    {
        var result = await GetAnswering(new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(body) });

        result.IsFailure.ShouldBeTrue();
        result.Code!.FormatClientMessage(result.Details).ShouldBeNull();
    }
}
