using System.Net;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Fdw.Web.RestEndpoints.ApiReference;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Web.RestEndpoints.Tests.ApiReference;

/// <summary>
/// FDW-1047: a reference document names the server it describes, built from the request that fetched it. A reader such as
/// Scalar builds every request URL from it, and a document with none leaves the browser unable to construct one.
/// </summary>
[Collection(nameof(RestEndpointsTestCollection))]
public sealed class ApiReferenceServerTests
{
    private const string Document = """{"openapi":"3.0.0","info":{"title":"t"},"paths":{"/api/v1/things":{"get":{}}}}""";

    private static ApiReferenceDocument Parse(string json) => ApiReferenceDocument.Parse(json, NullLogger.Instance).Value!;

    private static string ServerUrl(ApiReferenceDocument document)
        => JsonNode.Parse(document.ToJson())!["servers"]![0]!["url"]!.GetValue<string>();

    private static DefaultHttpContext Fetched(string scheme, string host, string pathBase = "")
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.7");
        context.Request.Scheme = scheme;
        context.Request.Host = new HostString(host);
        context.Request.PathBase = new PathString(pathBase);
        return context;
    }

    // The middleware the host runs (FdwReferenceHosts.ForwardedHeaders): it trusts any upstream when no network is declared.
    private static async Task Forward(DefaultHttpContext context)
    {
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
        };
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
        await new ForwardedHeadersMiddleware(_ => Task.CompletedTask, NullLoggerFactory.Instance, Options.Create(options))
            .Invoke(context);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void TheServerIsTheOriginTheRequestReachedTheHostBy()
    {
        var document = Parse(Document);

        document.SetServer(Fetched("http", "internal-host:8080").Request, NullLogger.Instance).IsSuccess.ShouldBeTrue();

        ServerUrl(document).ShouldBe("http://internal-host:8080");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ForwardedProtoAndHostFromAFrontEndDecideTheServer()
    {
        var context = Fetched("http", "internal-host:8080");
        context.Request.Headers["X-Forwarded-Proto"] = "https";
        context.Request.Headers["X-Forwarded-Host"] = "abacus-slot.example.com";
        await Forward(context);
        var document = Parse(Document);

        document.SetServer(context.Request, NullLogger.Instance).IsSuccess.ShouldBeTrue();

        ServerUrl(document).ShouldBe("https://abacus-slot.example.com");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task WithNoForwardedHeadersTheServerIsWhatTheHostSaw()
    {
        var context = Fetched("http", "internal-host:8080");
        await Forward(context);
        var document = Parse(Document);

        document.SetServer(context.Request, NullLogger.Instance);

        ServerUrl(document).ShouldBe("http://internal-host:8080");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ThePathBaseIsPartOfTheServerAndTheApiPrefixIsNot()
    {
        var document = Parse(Document);

        document.SetServer(Fetched("https", "example.com", "/slot").Request, NullLogger.Instance);

        ServerUrl(document).ShouldBe("https://example.com/slot");
        ServerUrl(document).ShouldNotContain("/api/v1");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AServersEntryTheGeneratorWroteIsReplacedNotAddedTo()
    {
        var document = Parse("""{"openapi":"3.0.0","servers":[{"url":"http://localhost:5000"},{"url":"http://other"}],"paths":{}}""");

        document.SetServer(Fetched("https", "example.com").Request, NullLogger.Instance);

        var servers = JsonNode.Parse(document.ToJson())!["servers"]!.AsArray();
        servers.Count.ShouldBe(1);
        servers[0]!["url"]!.GetValue<string>().ShouldBe("https://example.com");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void ARequestThatNamesNoHostIsRefusedRatherThanGivenAnInvalidServer()
    {
        var context = new DefaultHttpContext();
        var document = Parse(Document);

        document.SetServer(context.Request, NullLogger.Instance).IsFailure.ShouldBeTrue();

        JsonNode.Parse(document.ToJson())!["servers"].ShouldBeNull();
    }
}
