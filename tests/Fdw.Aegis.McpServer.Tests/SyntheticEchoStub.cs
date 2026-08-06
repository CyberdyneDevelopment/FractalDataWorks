using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
// Why: the Fdw.* project tree nests everything under the "Fdw" root namespace, so an unqualified
// "Results" in a file under Fdw.Aegis.McpServer.Tests resolves to the sibling Fdw.Results namespace
// (C# checks enclosing-namespace members before "using" directives) rather than
// Microsoft.AspNetCore.Http.Results. Alias it explicitly to the ASP.NET Core static helper.
using WebResults = Microsoft.AspNetCore.Http.Results;

namespace Fdw.Aegis.McpServer.Tests;

/// <summary>
/// An in-process Kestrel stub standing in for the downstream HTTP endpoint Aegis brokers a
/// credential to. It records — server-side — a SHA256 fingerprint of the exact <c>Authorization</c>
/// header it received, so a test can prove the real credential arrived WITHOUT that proof depending
/// on anything the gateway returns to the caller.
/// </summary>
/// <remarks>
/// Why the <paramref name="hostile"/> mode: the whole non-exposure guarantee is that Aegis never
/// passes downstream-derived content back to Claude. A polite stub can't test that — it never tries
/// to leak. A hostile stub deliberately <em>echoes the received Authorization header (the plaintext
/// token) in its own 200 response body</em>, so the proof can assert the gateway still surfaces none
/// of it. This is the adversarial case the first proof harness lacked.
/// </remarks>
public sealed class SyntheticEchoStub : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly bool _hostile;
    private int _requestCount;
    private volatile string _lastAuthorizationFingerprint = string.Empty;

    private SyntheticEchoStub(WebApplication app, bool hostile)
    {
        _app = app;
        _hostile = hostile;
    }

    /// <summary>Gets the stub's actual bound base address (dynamic port).</summary>
    public string Address { get; private set; } = string.Empty;

    /// <summary>Gets the number of requests the stub has received so far.</summary>
    public int RequestCount => Volatile.Read(ref _requestCount);

    /// <summary>
    /// Gets the SHA256 fingerprint (first 8 hex chars) of the most recent <c>Authorization</c> header
    /// the stub received — recorded server-side, so a test proves the real credential arrived without
    /// reading anything the gateway returned to the caller.
    /// </summary>
    public string LastAuthorizationFingerprint => _lastAuthorizationFingerprint;

    /// <summary>Computes the fingerprint the stub would record for a given raw header value.</summary>
    public static string FingerprintOf(string headerValue) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(headerValue)))[..8].ToLowerInvariant();

    /// <summary>Starts a fresh stub on a dynamically assigned loopback port.</summary>
    /// <param name="hostile">When true, the stub echoes the received Authorization header verbatim in
    /// its response body — an adversarial downstream that tries to reflect the credential back.</param>
    public static async Task<SyntheticEchoStub> Start(bool hostile = false)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();

        var stub = new SyntheticEchoStub(app, hostile);

        app.MapGet("/", (HttpContext context) =>
        {
            Interlocked.Increment(ref stub._requestCount);

            var authHeader = context.Request.Headers.Authorization.ToString();
            stub._lastAuthorizationFingerprint = FingerprintOf(authHeader);

            // Why: the hostile branch returns the raw credential in the body on purpose — the test
            // asserts the gateway surfaces none of it. The polite branch returns only a fingerprint.
            return stub._hostile
                ? WebResults.Json(new { received = true, echoed = authHeader })
                : WebResults.Json(new { received = true, fingerprint = stub._lastAuthorizationFingerprint });
        });

        await app.StartAsync().ConfigureAwait(false);

        stub.Address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.First();

        return stub;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync().ConfigureAwait(false);
        await _app.DisposeAsync().ConfigureAwait(false);
    }
}
