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
}
