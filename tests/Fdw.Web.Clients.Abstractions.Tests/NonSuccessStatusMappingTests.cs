using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.Clients.Abstractions.Tests;

/// <summary>
/// Unit tests for FDW-851 -- a non-success response carries a result code naming the actual
/// HTTP status band, instead of every non-success answering the same fixed way.
/// </summary>
public sealed class NonSuccessStatusMappingTests
{
    private static readonly Uri BaseUri = new("https://test.example.com/");

    private static TestApiClient CreateClient(HttpStatusCode status)
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(status));
        var httpClient = new HttpClient(handler) { BaseAddress = BaseUri };
        return new TestApiClient(httpClient, Mock.Of<ILogger<ApiClientBase>>());
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    [InlineData(HttpStatusCode.BadRequest, "UpstreamRequestInvalid")]
    [InlineData(HttpStatusCode.Unauthorized, "UpstreamAuthenticationFailed")]
    [InlineData(HttpStatusCode.Forbidden, "UpstreamForbidden")]
    [InlineData(HttpStatusCode.NotFound, "UpstreamResourceNotFound")]
    [InlineData(HttpStatusCode.Conflict, "UpstreamConflict")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "UpstreamUnavailable")]
    [InlineData(HttpStatusCode.GatewayTimeout, "UpstreamGatewayTimeout")]
    [InlineData(HttpStatusCode.InternalServerError, "UpstreamInternalError")]
    [InlineData(HttpStatusCode.BadGateway, "UpstreamCallFailed")]
    public async Task PostNonSuccessStatusMapsToTheMatchingCode(HttpStatusCode status, string expectedCodeName)
    {
        var sut = CreateClient(status);

        var result = await sut.Post<TestModel, TestModel>("api/items", new TestModel(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code!.Name.ShouldBe(expectedCodeName);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task GetNonSuccessStatusMapsToTheMatchingCode()
    {
        // Get<T> goes through GetFromJsonAsync, which throws instead of returning a response the
        // rest of ApiClientBase checks IsSuccessStatusCode on -- a separate path from every other
        // verb here, and the one FDW-851 originally missed.
        var sut = CreateClient(HttpStatusCode.NotFound);

        var result = await sut.Get<TestModel>("api/items/missing", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code!.Name.ShouldBe("UpstreamResourceNotFound");
    }
}
