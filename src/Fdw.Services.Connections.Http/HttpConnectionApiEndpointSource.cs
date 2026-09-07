using System.Threading;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Http.Logging;
using Fdw.Web.Http.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Http;

/// <summary>
/// Resolves an API client's base address from the HTTP connection declared under the same name.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IApiEndpointSource"/> had no implementation anywhere, so
/// <c>ApiEndpointRegistration.ResolveEndpoint</c> returned null for every client and every one
/// registered through <c>AddApiHttpClient</c> was handed out with no BaseAddress -- Pipelines,
/// Settings, SecretManagers, Messaging, Users, Search, Authentication, Notifications, DataStores,
/// Agents and Schedules alike. The visible symptom was <c>/api/v1/proxy/schedules</c> returning
/// 502 with "An invalid request URI was provided", which reads as the far side being down rather
/// than the near side never having been told where to look.
/// </para>
/// <para>
/// The contract was already stated by the failure it produced: <c>EndpointNotDeclared</c> tells
/// the operator to configure "the HTTP connection named {clientName}". So a client named
/// ScheduleClient is served by the conn.Connection row named ScheduleClient, and its
/// implementation's BaseUrl is the answer.
/// </para>
/// <para>
/// Null stays a real answer rather than a failure. A host registers a client for every package it
/// references and calls only a few -- reference-api registers around thirty-five and uses two --
/// so demanding an endpoint for each would force an operator to declare thirty-three URLs nobody
/// resolves, and the only way to satisfy that is to invent them. Null means "nothing declares
/// one", and the caller reports it through <c>EndpointNotDeclared</c> naming the client.
/// </para>
/// </remarks>
public sealed class HttpConnectionApiEndpointSource : IApiEndpointSource
{
    private readonly IConnectionConfigurationProvider _connections;

    private readonly ILogger _log;

    /// <summary>Initializes a new instance of the <see cref="HttpConnectionApiEndpointSource"/> class.</summary>
    /// <param name="connections">The connection configuration provider.</param>
    /// <param name="logger">The logger.</param>
    public HttpConnectionApiEndpointSource(
        IConnectionConfigurationProvider connections,
        ILogger<HttpConnectionApiEndpointSource>? logger = null)
    {
        _connections = connections;
        _log = logger ?? NullLogger<HttpConnectionApiEndpointSource>.Instance;
    }

    /// <inheritdoc />
    public string? Resolve(string clientName)
    {
        if (string.IsNullOrEmpty(clientName))
            return null;

        // Why the connection provider rather than a dedicated store: an endpoint IS a connection
        // here. IConnectionConfigurationProvider is a domain provider, so Get(name) returns the
        // IMPLEMENTATION for the row of that name -- the Http one carries BaseUrl.
        //
        // Why sync-over-async: IApiEndpointSource.Resolve is synchronous because it is consulted
        // from inside AddHttpClient's configure delegate, which the factory runs per CreateClient
        // and which has no asynchronous form to hand.
#pragma warning disable VSTHRD002
        var result = _connections.Get(clientName, CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002

        if (!result.IsSuccess || result.Value is null)
        {
            HttpApiEndpointSourceLog.NoConnectionForClient(_log, clientName);
            return null;
        }

        // A connection of another kind under this name is a declaration that means something else,
        // not an endpoint, so it is reported rather than coerced into one.
        if (result.Value is not HttpConnectionConfigurationBase http)
        {
            HttpApiEndpointSourceLog.ConnectionIsNotHttp(
                _log, clientName, result.Value.GetType().Name);
            return null;
        }

        if (string.IsNullOrWhiteSpace(http.BaseUrl))
        {
            HttpApiEndpointSourceLog.ConnectionHasNoBaseUrl(_log, clientName);
            return null;
        }

        HttpApiEndpointSourceLog.EndpointResolved(_log, clientName, http.BaseUrl);
        return http.BaseUrl;
    }
}
