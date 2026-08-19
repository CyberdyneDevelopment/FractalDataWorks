using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Abstractions.Methods;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.WebMcp.Hosting;

/// <summary>
/// Middleware that intercepts <c>Authorization: Bearer fdx_*</c> headers and validates them
/// as Personal Access Tokens via <see cref="IPersonalAccessTokenService"/>.
/// Tokens that do not start with <c>Bearer fdx_</c> are passed through to the next handler
/// (e.g., JWT bearer).
/// </summary>
public sealed class WebMcpApiKeyMiddleware
{
    private const string BearerPrefix = "Bearer ";
    private const string PatPrefix = "Bearer fdx_";

    private readonly RequestDelegate _next;
    private readonly ILogger<WebMcpApiKeyMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebMcpApiKeyMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware delegate.</param>
    /// <param name="logger">The logger instance.</param>
    public WebMcpApiKeyMiddleware(RequestDelegate next, ILogger<WebMcpApiKeyMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(logger);

        _next = next;
        _logger = logger ?? NullLogger<WebMcpApiKeyMiddleware>.Instance;
    }

    /// <summary>Invokes the middleware.</summary>
    public async Task Invoke(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.ToString();

        if (!authHeader.StartsWith(PatPrefix, StringComparison.OrdinalIgnoreCase))
        {
            // Not a PAT — let the next handler (JWT bearer) process it
            await _next(context).ConfigureAwait(false);
            return;
        }

        WebMcpApiKeyMiddlewareLog.PatHeaderDetected(_logger);

        var patService = context.RequestServices.GetService<IPersonalAccessTokenService>();

        if (patService is null)
        {
            WebMcpApiKeyMiddlewareLog.PatServiceNotRegistered(_logger);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var rawToken = authHeader.Substring(BearerPrefix.Length).Trim();

        var result = await patService.ValidateToken(rawToken, context.RequestAborted).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            WebMcpApiKeyMiddlewareLog.PatValidationError(_logger, result.CurrentMessage ?? "Validation service error");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var validation = result.Value;

        if (validation is null || !validation.IsValid)
        {
            WebMcpApiKeyMiddlewareLog.PatAuthenticationFailed(_logger);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        WebMcpApiKeyMiddlewareLog.PatAuthenticationSucceeded(_logger, validation.UserId.ToString());

        var identity = new ClaimsIdentity(
            new[] { new Claim("sub", validation.UserId.ToString()) },
            "PATBearer");
        context.User = new ClaimsPrincipal(identity);

        await _next(context).ConfigureAwait(false);
    }
}
