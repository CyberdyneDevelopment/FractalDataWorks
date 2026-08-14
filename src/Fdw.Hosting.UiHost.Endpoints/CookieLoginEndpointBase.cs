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
/// Exchanges a posted login form for an API token and stores it on the Blazor cookie.
/// </summary>
/// <remarks>
/// The exchange itself is <see cref="CookieSignInRoutes.SignIn"/> — unchanged, and still shared with
/// any host that maps the routes directly. This type exists so the endpoint collection can count it.
/// </remarks>
public abstract class CookieLoginEndpointBase : EndpointWithoutRequest
{
    private const string LoginRoute = "/auth/login";

    private readonly CookieSignInOptions options;
    private readonly IHttpClientFactory clients;
    private readonly ILoggerFactory? loggerFactory;
    private readonly ILogger<CookieLoginEndpointBase> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CookieLoginEndpointBase"/> class.
    /// </summary>
    /// <param name="options">The values this deployment supplies.</param>
    /// <param name="clients">The client factory.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    protected CookieLoginEndpointBase(
        CookieSignInOptions options,
        IHttpClientFactory clients,
        ILoggerFactory? loggerFactory)
    {
        this.options = options;
        this.clients = clients;
        this.loggerFactory = loggerFactory;
        logger = loggerFactory?.CreateLogger<CookieLoginEndpointBase>()
                 ?? NullLogger<CookieLoginEndpointBase>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post(LoginRoute);

        // Why anonymous: this is the route a caller uses to stop being anonymous.
        AllowAnonymous();

        // Why form data with no antiforgery token: the login form is posted by a caller who has no
        // token yet, which is the one place the check cannot apply.
        AllowFormData();

        CookieSignInEndpointLog.LoginEndpointConfigured(logger, LoginRoute);
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        CookieSignInEndpointLog.LoginHandling(logger);
        var started = Stopwatch.GetTimestamp();

        await CookieSignInRoutes.SignIn(HttpContext, clients, options, loggerFactory).ConfigureAwait(false);

        CookieSignInEndpointLog.LoginHandled(
            logger, (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds);
    }
}
