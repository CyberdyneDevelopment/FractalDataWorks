using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Hosting.UiHost.Authentication;
using Microsoft.Extensions.Logging;

namespace Fdw.Hosting.UiHost.Endpoints;

/// <summary>
/// Clears the Blazor cookie and asks the server to revoke the refresh token.
/// </summary>
/// <remarks>
/// The work is <see cref="CookieSignInRoutes.SignOut"/>; this type is the declaration.
/// </remarks>
public sealed class CookieLogoutEndpoint : EndpointWithoutRequest
{
    private readonly CookieSignInOptions options;
    private readonly IHttpClientFactory clients;
    private readonly ILoggerFactory? loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="CookieLogoutEndpoint"/> class.
    /// </summary>
    /// <param name="options">The values this deployment supplies.</param>
    /// <param name="clients">The client factory.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public CookieLogoutEndpoint(
        CookieSignInOptions options,
        IHttpClientFactory clients,
        ILoggerFactory? loggerFactory)
    {
        this.options = options;
        this.clients = clients;
        this.loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/auth/logout");

        // Why anonymous rather than authorized: signing out an expired or already-cleared session
        // has to succeed, or a caller whose cookie has gone bad cannot get back to a clean state.
        AllowAnonymous();
    }

    /// <inheritdoc />
    public override Task HandleAsync(CancellationToken ct) =>
        CookieSignInRoutes.SignOut(HttpContext, clients, options, loggerFactory);
}
