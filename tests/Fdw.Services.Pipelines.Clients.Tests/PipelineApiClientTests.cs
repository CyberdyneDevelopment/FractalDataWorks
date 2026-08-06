using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Clients.Tests;

public sealed class PipelineJobHttpClientTests
{
    private static readonly Uri BaseUri = new("https://test.example.com/");

    private static PipelineJobHttpClient CreateClient(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = BaseUri };
        return new PipelineJobHttpClient(httpClient, Mock.Of<ILogger<PipelineJobHttpClient>>());
    }

    // --- Trigger ---

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task TriggerSendsPostRequestToCorrectPath()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new TriggerPipelineResponse())
        });
        var sut = CreateClient(handler);

        await sut.Trigger(new TriggerPipelineRequest { Name = "test-pipeline" }, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        // Why: the client targets the ETL's canonical UnifiedTriggerEndpoint route (POST
        // etl/trigger/pipeline). The old "proxy/etl/trigger" target is hosted only by the
        // reference-api inbound proxy, never the ETL, so dispatch 404'd against the ETL.
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/etl/trigger/pipeline");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task TriggerReturnsDeserializedResponseOnSuccess()
    {
        var executionId = Guid.NewGuid();
        var response = new TriggerPipelineResponse { ExecutionId = executionId, Status = "Running" };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(response)
        });
        var sut = CreateClient(handler);

        var result = await sut.Trigger(new TriggerPipelineRequest { Name = "test-pipeline" }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.ExecutionId.ShouldBe(executionId);
        result.Value.Status.ShouldBe("Running");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task TriggerReturnsFailureOnNonSuccessStatus()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest));
        var sut = CreateClient(handler);

        var result = await sut.Trigger(new TriggerPipelineRequest(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task TriggerReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var sut = CreateClient(handler);

        var result = await sut.Trigger(new TriggerPipelineRequest(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    // --- GetStatus ---

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetStatusSendsGetRequestToCorrectPath()
    {
        var executionId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new TriggerPipelineResponse())
        });
        var sut = CreateClient(handler);

        await sut.GetStatus(executionId, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe($"/etl/jobs/{executionId}/status");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetStatusReturnsDeserializedResponse()
    {
        var executionId = Guid.NewGuid();
        var response = new TriggerPipelineResponse
        {
            ExecutionId = executionId,
            Status = "Completed"
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(response)
        });
        var sut = CreateClient(handler);

        var result = await sut.GetStatus(executionId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.ExecutionId.ShouldBe(executionId);
        result.Value.Status.ShouldBe("Completed");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetStatusReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var sut = CreateClient(handler);

        var result = await sut.GetStatus(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }
}
