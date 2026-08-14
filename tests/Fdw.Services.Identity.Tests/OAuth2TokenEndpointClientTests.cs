using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Identity.Authentik;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Identity.Tests;

/// <summary>
/// Tests for <see cref="OAuth2TokenEndpointClient"/>, stubbing the HTTP boundary.
/// </summary>
/// <remarks>
/// The error-response shape asserted here is the one the live Authentik at
/// <c>login.cyberdynedevelopment.dev</c> actually returns for an unknown client:
/// <c>{"error":"invalid_client","error_description":"..."}</c> with HTTP 400.
/// </remarks>
public class OAuth2TokenEndpointClientTests
{
    private const string Endpoint = "https://login.example.dev/application/o/token/";
    private const string Issuer = "https://login.example.dev/application/o/etl/";

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public FormUrlEncodedContent? Captured { get; private set; }
        public Dictionary<string, string> Form { get; } = new(StringComparer.Ordinal);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Content is not null)
            {
                foreach (var pair in (await request.Content.ReadAsStringAsync(cancellationToken)).Split('&'))
                {
                    var parts = pair.Split('=', 2);
                    if (parts.Length == 2)
                        Form[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1].Replace('+', ' '));
                }
            }

            return new HttpResponseMessage(status) { Content = new StringContent(body) };
        }
    }

    private static (OAuth2TokenEndpointClient Client, StubHandler Handler) Client(HttpStatusCode status, string body)
    {
        var handler = new StubHandler(status, body);
        return (new OAuth2TokenEndpointClient(new HttpClient(handler), NullLogger.Instance), handler);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ExchangeReturnsTheIssuedToken()
    {
        var (client, _) = Client(HttpStatusCode.OK,
            """{"access_token":"the-token","token_type":"Bearer","expires_in":300,"scope":"read write"}""");

        var result = await client.Exchange("SchedulerIdentity", Endpoint, Issuer,
            new IdentityTokenRequest("https://etl.example.dev", ["read", "write"]),
            new Dictionary<string, string>(StringComparer.Ordinal) { ["client_id"] = "cid", ["client_secret"] = "shh" },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe("the-token");
        result.Value.TokenType.ShouldBe("Bearer");
        result.Value.Audience.ShouldBe("https://etl.example.dev");
        result.Value.Scopes.ShouldBe(["read", "write"]);
        result.Value.ExpiresAt.ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddSeconds(280));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ExchangeSendsTheGrantAndTheCallersCredentialAndTheAudience()
    {
        var (client, handler) = Client(HttpStatusCode.OK,
            """{"access_token":"t","token_type":"Bearer","expires_in":300}""");

        await client.Exchange("SchedulerIdentity", Endpoint, Issuer,
            new IdentityTokenRequest("https://etl.example.dev", ["read"]),
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["client_id"] = "cid",
                ["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
                ["client_assertion"] = "the.jwt.assertion",
            },
            TestContext.Current.CancellationToken);

        handler.Form["grant_type"].ShouldBe("client_credentials");
        handler.Form["client_id"].ShouldBe("cid");
        handler.Form["client_assertion"].ShouldBe("the.jwt.assertion");
        handler.Form["audience"].ShouldBe("https://etl.example.dev");
        handler.Form["scope"].ShouldBe("read");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ExchangeReportsARejectedCredentialDistinctlyFromAProviderFault()
    {
        // This is the exact body the live Authentik returns for an unknown client.
        var (rejected, _) = Client(HttpStatusCode.BadRequest,
            """{"error":"invalid_client","error_description":"Client authentication failed (e.g., unknown client, no client authentication included, or unsupported authentication method)"}""");

        var rejection = await rejected.Exchange("SchedulerIdentity", Endpoint, Issuer,
            new IdentityTokenRequest("https://etl.example.dev"),
            new Dictionary<string, string>(StringComparer.Ordinal) { ["client_id"] = "cid" },
            TestContext.Current.CancellationToken);

        rejection.IsFailure.ShouldBeTrue();
        rejection.CurrentMessage!.ShouldContain("rejected this service's credential");

        var (down, _) = Client(HttpStatusCode.ServiceUnavailable, "upstream unavailable");
        var fault = await down.Exchange("SchedulerIdentity", Endpoint, Issuer,
            new IdentityTokenRequest("https://etl.example.dev"),
            new Dictionary<string, string>(StringComparer.Ordinal) { ["client_id"] = "cid" },
            TestContext.Current.CancellationToken);

        fault.IsFailure.ShouldBeTrue();
        fault.CurrentMessage!.ShouldContain("503");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData("""{"token_type":"Bearer","expires_in":300}""", "access_token")]
    [InlineData("""{"access_token":"t","token_type":"Bearer"}""", "expires_in")]
    [InlineData("""{"access_token":"t","expires_in":300}""", "token_type")]
    public async Task ExchangeFailsLoudWhenTheResponseIsIncomplete(string body, string missingField)
    {
        // NO FALLBACKS: a missing token_type is not silently assumed to be Bearer, and a missing
        // expires_in is not silently treated as "never expires".
        var (client, _) = Client(HttpStatusCode.OK, body);

        var result = await client.Exchange("SchedulerIdentity", Endpoint, Issuer,
            new IdentityTokenRequest("https://etl.example.dev"),
            new Dictionary<string, string>(StringComparer.Ordinal) { ["client_id"] = "cid" },
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage!.ShouldContain(missingField);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task ExchangeFailsWhenTheResponseIsNotJson()
    {
        var (client, _) = Client(HttpStatusCode.OK, "<html>a proxy error page</html>");

        var result = await client.Exchange("SchedulerIdentity", Endpoint, Issuer,
            new IdentityTokenRequest("https://etl.example.dev"),
            new Dictionary<string, string>(StringComparer.Ordinal) { ["client_id"] = "cid" },
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
    }
}
