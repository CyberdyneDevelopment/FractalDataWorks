using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Hosting.UiHost.Authentication;
using Microsoft.Extensions.Logging;

namespace Fdw.Hosting.UiHost.Endpoints;

/// <summary>
/// Exchanges a posted login form for an API token and stores it on the Blazor cookie.
/// </summary>
/// <remarks>
/// The exchange itself is <see cref="CookieSignInRoutes.SignIn"/> — unchanged, and still shared with
/// any host that maps the routes directly. This type exists so the endpoint collection can count it.
/// </remarks>
public sealed class CookieLoginEndpoint : EndpointWithoutRequest
{
    private readonly CookieSignInOptions options;
    private readonly IHttpClientFactory clients;
    private readonly ILoggerFactory? loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="CookieLoginEndpoint"/> class.
    /// </summary>
    /// <param name="options">The values this deployment supplies.</param>
    /// <param name="clients">The client factory.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public CookieLoginEndpoint(
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
        Post("/auth/login");

        // Why anonymous: this is the route a caller uses to stop being anonymous.
        AllowAnonymous();

        // Why form data with no antiforgery token: the login form is posted by a caller who has no
        // token yet, which is the one place the check cannot apply.
        AllowFormData();
    }

    /// <inheritdoc />
    public override Task HandleAsync(CancellationToken ct) =>
        CookieSignInRoutes.SignIn(HttpContext, clients, options, loggerFactory);
}
