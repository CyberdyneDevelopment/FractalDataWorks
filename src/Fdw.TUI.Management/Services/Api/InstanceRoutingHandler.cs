using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.TUI.Management.Services.Api;

/// <summary>
/// Re-targets every API-client request at the Fdw instance the user is currently connected to.
/// </summary>
/// <remarks>
/// <para>
/// The shared client options (<c>ConnectionClientType</c> and friends) bake a <c>BaseAddress</c> into
/// their named <c>HttpClient</c> at registration time, from <c>ApiClients:BaseUrl</c>. That suits a host
/// with one fixed API. The TUI is different: it chooses its instance at runtime and can switch between
/// them, so the registered base address is only a placeholder and this handler rewrites each request
/// onto the live instance. That keeps every existing API client usable without rebuilding the container.
/// </para>
/// <para>
/// When nothing is connected the request is refused with a synthetic 503 rather than being sent to the
/// placeholder host — fail loud, and let the client's result surface it, instead of silently querying
/// the wrong server.
/// </para>
/// </remarks>
public sealed class InstanceRoutingHandler : DelegatingHandler
{
    private readonly IConnectionManager _connectionManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="InstanceRoutingHandler"/> class.
    /// </summary>
    public InstanceRoutingHandler(IConnectionManager connectionManager)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var status = _connectionManager.GetStatus();
        if (!status.IsConnected || string.IsNullOrWhiteSpace(status.Url))
        {
            return Task.FromResult(NotConnected(request));
        }

        if (!Uri.TryCreate(ConnectionManager.NormalizeBase(status.Url), UriKind.Absolute, out var baseUri))
        {
            return Task.FromResult(NotConnected(request));
        }

        // Why PathAndQuery: the registered placeholder base contributed only the authority, so carry the
        // client's own path and query across to the live instance untouched.
        if (request.RequestUri is not null)
        {
            request.RequestUri = new Uri(baseUri, request.RequestUri.PathAndQuery.TrimStart('/'));
        }

        return base.SendAsync(request, cancellationToken);
    }

    private static HttpResponseMessage NotConnected(HttpRequestMessage request) =>
        new(HttpStatusCode.ServiceUnavailable)
        {
            RequestMessage = request,
            ReasonPhrase = "No Fdw instance is connected.",
        };
}
