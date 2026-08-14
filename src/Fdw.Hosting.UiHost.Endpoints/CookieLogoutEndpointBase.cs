using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Hosting.UiHost.Authentication;
using Fdw.Hosting.UiHost.Endpoints.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Hosting.UiHost.Endpoints;

/// <summary>
/// Clears the Blazor cookie and asks the server to revoke the refresh token.
/// </summary>
/// <remarks>
/// The work is <see cref="CookieSignInRoutes.SignOut"/>; this type is the declaration.
/// </remarks>
public abstract class CookieLogoutEndpointBase : EndpointWithoutRequest
{
    private const string LogoutRoute = "/auth/logout";

    private readonly CookieSignInOptions options;
    private readonly IHttpClientFactory clients;
    private readonly ILoggerFactory? loggerFactory;
    private readonly ILogger<CookieLogoutEndpointBase> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CookieLogoutEndpointBase"/> class.
    /// </summary>
    /// <param name="options">The values this deployment supplies.</param>
    /// <param name="clients">The client factory.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    protected CookieLogoutEndpointBase(
        CookieSignInOptions options,
        IHttpClientFactory clients,
        ILoggerFactory? loggerFactory)
    {
        this.options = options;
        this.clients = clients;
        this.loggerFactory = loggerFactory;
        logger = loggerFactory?.CreateLogger<CookieLogoutEndpointBase>()
                 ?? NullLogger<CookieLogoutEndpointBase>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get(LogoutRoute);

        // Why anonymous rather than authorized: signing out an expired or already-cleared session
        // has to succeed, or a caller whose cookie has gone bad cannot get back to a clean state.
        AllowAnonymous();

        CookieSignInEndpointLog.LogoutEndpointConfigured(logger, LogoutRoute);
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        CookieSignInEndpointLog.LogoutHandling(logger);
        var started = Stopwatch.GetTimestamp();

        await CookieSignInRoutes.SignOut(HttpContext, clients, options, loggerFactory).ConfigureAwait(false);

        CookieSignInEndpointLog.LogoutHandled(
            logger, (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds);
    }
}
