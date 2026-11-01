using System.Net;
using System.Net.Http;
using System.Text;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.Clients.Abstractions.Tests;

/// <summary>
/// FDW-1132 -- a downstream 500 stays an internal error and the client keeps the downstream's own <c>referenceId</c> and
/// <c>code</c>, so a host that relays the failure answers with the downstream's status and one id for the whole chain.
/// </summary>
public sealed class UpstreamProblemBodyTests
{
    private const string ReferenceId = "0HNP3VG25HU9A";
    private const string UpstreamCode = "SERVICES-61001";

    private static readonly string ProblemBody =
        "{\"title\":\"A service configuration error occurred\",\"status\":500,\"detail\":\"A service configuration error occurred\","
        + $"\"code\":\"{UpstreamCode}\",\"referenceId\":\"{ReferenceId}\"}}";

    private static TestApiClient CreateClient(HttpStatusCode status, string body, string mediaType)
    {
        var response = new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, mediaType) };
        var httpClient = new HttpClient(new MockHttpMessageHandler(response)) { BaseAddress = new Uri("https://test.example.com/") };
        return new TestApiClient(httpClient, Mock.Of<ILogger<ApiClientBase>>());
    }

    private static object? Captured(IGenericResult result, string key)
        => result.Details is { } details && details.Data.TryGetValue(key, out var value) ? value : null;

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AnInternalErrorProblemBodyKeepsTheStatusBandAndTheDownstreamIds()
    {
        var sut = CreateClient(HttpStatusCode.InternalServerError, ProblemBody, "application/problem+json");

        var result = await sut.Post<TestModel, TestModel>("api/items", new TestModel(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("UpstreamInternalError");
        Captured(result, ResultDetailKeys.UpstreamReferenceId).ShouldBe(ReferenceId);
        Captured(result, ResultDetailKeys.UpstreamCode).ShouldBe(UpstreamCode);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ARefusalProblemBodyKeepsItsReasonAndTheDownstreamId()
    {
        var body = "{\"title\":\"The request was invalid\",\"status\":400,\"detail\":\"Name is required\","
            + $"\"code\":\"CLIENT-20000\",\"referenceId\":\"{ReferenceId}\"}}";
        var sut = CreateClient(HttpStatusCode.BadRequest, body, "application/problem+json");

        var result = await sut.Post<TestModel, TestModel>("api/items", new TestModel(), TestContext.Current.CancellationToken);

        result.Code.ShouldNotBeNull().Name.ShouldBe("UpstreamRequestInvalid");
        Captured(result, "reason").ShouldBe("Name is required");
        Captured(result, ResultDetailKeys.UpstreamReferenceId).ShouldBe(ReferenceId);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ABodyThatIsNotAProblemBodyCapturesNoDownstreamIds()
    {
        var sut = CreateClient(HttpStatusCode.BadGateway, "error code: 502", "text/plain");

        var result = await sut.Post<TestModel, TestModel>("api/items", new TestModel(), TestContext.Current.CancellationToken);

        result.Code.ShouldNotBeNull().Name.ShouldBe("UpstreamCallFailed");
        Captured(result, ResultDetailKeys.UpstreamReferenceId).ShouldBeNull();
        Captured(result, ResultDetailKeys.UpstreamCode).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AConflictProblemBodyHandsItsReasonToTheCaller()
    {
        var body = "{\"title\":\"The request conflicted with the current state of the resource\",\"status\":409,\"detail\":\"The pipeline 'AfcPlayersLoad' is counted by the threshold 'Backlog' of Server\","
            + $"\"code\":\"WAITTHRESHOLD-42160\",\"referenceId\":\"{ReferenceId}\"}}";
        var sut = CreateClient(HttpStatusCode.Conflict, body, "application/problem+json");

        var result = await sut.Post<TestModel, TestModel>("api/items", new TestModel(), TestContext.Current.CancellationToken);

        result.Code.ShouldNotBeNull().Name.ShouldBe("UpstreamConflict");
        Captured(result, "reason").ShouldBe("The pipeline 'AfcPlayersLoad' is counted by the threshold 'Backlog' of Server");
        result.Code.FormatClientMessage(result.Details).ShouldBe("The pipeline 'AfcPlayersLoad' is counted by the threshold 'Backlog' of Server");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ARefusedTriggerBodyThatStatesReasonAndNotDetailHandsItsReasonToTheCaller()
    {
        var body = "{\"executionId\":\"01a18cdc-fc5f-7f15-9aa4-fc11aa47a0df\",\"status\":\"ServerBusy\",\"reason\":\"The server is not taking new runs: the threshold 'Queue full' of Server fires\"}";
        var sut = CreateClient(HttpStatusCode.ServiceUnavailable, body, "application/json");

        var result = await sut.Post<TestModel, TestModel>("api/items", new TestModel(), TestContext.Current.CancellationToken);

        result.Code.ShouldNotBeNull().Name.ShouldBe("UpstreamUnavailable");
        result.Code.FormatClientMessage(result.Details).ShouldBe("The server is not taking new runs: the threshold 'Queue full' of Server fires");
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "<html>Service Unavailable</html>")]
    [InlineData(HttpStatusCode.BadGateway, "{\"reason\":\"upstream said so\"}")]
    public async Task AnAnswerWithNoReasonOrACallThatFailedHandsNoReasonToTheCaller(HttpStatusCode status, string body)
    {
        var sut = CreateClient(status, body, status == HttpStatusCode.ServiceUnavailable ? "text/html" : "application/json");

        var result = await sut.Post<TestModel, TestModel>("api/items", new TestModel(), TestContext.Current.CancellationToken);

        result.Code.ShouldNotBeNull().FormatClientMessage(result.Details).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ARefusedTriggerBodyKeepsTheIdOfTheExecutionItClosed()
    {
        var body = "{\"executionId\":\"01a18cdc-fc5f-7f15-9aa4-fc11aa47a0df\",\"status\":\"ServerBusy\",\"reason\":\"The server is not taking new runs\"}";
        var sut = CreateClient(HttpStatusCode.ServiceUnavailable, body, "application/json");

        var result = await sut.Post<TestModel, TestModel>("api/items", new TestModel(), TestContext.Current.CancellationToken);

        Captured(result, ResultDetailKeys.ExecutionId).ShouldBe("01a18cdc-fc5f-7f15-9aa4-fc11aa47a0df");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AnAnswerWithNoExecutionIdKeepsNone()
    {
        var sut = CreateClient(HttpStatusCode.ServiceUnavailable, ProblemBody, "application/problem+json");

        var result = await sut.Post<TestModel, TestModel>("api/items", new TestModel(), TestContext.Current.CancellationToken);

        Captured(result, ResultDetailKeys.ExecutionId).ShouldBeNull();
    }
}
