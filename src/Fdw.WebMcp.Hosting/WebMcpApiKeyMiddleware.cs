using System;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Methods;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.WebMcp.Hosting;

/// <summary>
/// Authenticates <c>Authorization: Bearer fdx_*</c> credentials — agent keys and personal access
/// tokens — and hands anything else to the next handler (for example JWT bearer).
/// </summary>
/// <remarks>
/// Both credentials share the <c>fdx_</c> prefix because both are minted by the same generator; the
/// environment segment is what separates them, and <c>agent</c> is reserved for keys minted by
/// <see cref="IAgentKeyService"/>. They are told apart HERE rather than by trying one service and
/// falling back to the other: a fallback would report an unrecognised agent key as a bad PAT, and
/// the operator looking at the log could not tell which credential actually failed.
///
/// An agent acts on behalf of its owner, so its <c>sub</c> IS that person's — which is exactly why
/// the agent claims exist. Nothing downstream could otherwise distinguish an agent from the person
/// it acts for, and several things must: audit rows, message attribution, and any policy that gates
/// what an agent may do unattended.
/// </remarks>
public sealed class WebMcpApiKeyMiddleware
{
    private const string BearerPrefix = "Bearer ";
    private const string CredentialPrefix = "Bearer fdx_";
    private const string AgentKeyPrefix = "Bearer fdx_agent_";

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

        _next = next;
        _logger = logger ?? NullLogger<WebMcpApiKeyMiddleware>.Instance;
    }

    /// <summary>Invokes the middleware.</summary>
    /// <param name="context">The request context.</param>
    /// <returns>A task that completes when the request has been handled.</returns>
    public async Task Invoke(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.ToString();

        if (!authHeader.StartsWith(CredentialPrefix, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var rawCredential = authHeader.Substring(BearerPrefix.Length).Trim();

        var identity = authHeader.StartsWith(AgentKeyPrefix, StringComparison.OrdinalIgnoreCase)
            ? await AuthenticateAgentKey(context, rawCredential).ConfigureAwait(false)
            : await AuthenticatePat(context, rawCredential).ConfigureAwait(false);

        if (identity is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        context.User = new ClaimsPrincipal(identity);

        await _next(context).ConfigureAwait(false);
    }

    /// <summary>
    /// Validates an agent key and builds the identity it authenticates.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <param name="rawKey">The presented key.</param>
    /// <returns>The identity, or <see langword="null"/> when the key is not accepted.</returns>
    private async Task<ClaimsIdentity?> AuthenticateAgentKey(HttpContext context, string rawKey)
    {
        var service = context.RequestServices.GetService<IAgentKeyService>();

        if (service is null)
        {
            WebMcpApiKeyMiddlewareLog.AgentKeyServiceNotRegistered(_logger);
            return null;
        }

        var result = await service.ValidateKey(rawKey, context.RequestAborted).ConfigureAwait(false);

        if (result.IsFailure)
        {
            WebMcpApiKeyMiddlewareLog.AgentKeyValidationError(
                _logger,
                result.CurrentMessage ?? "Validation service error");
            return null;
        }

        if (result.Value is not { IsValid: true } validation)
        {
            WebMcpApiKeyMiddlewareLog.AgentKeyRejected(_logger, context.Request.Path);
            return null;
        }

        WebMcpApiKeyMiddlewareLog.AgentKeyAccepted(
            _logger,
            validation.Label,
            validation.UserId.ToString());

        // Why the agent claims sit beside sub rather than replacing it: the agent acts AS this user,
        // so every permission check, RLS predicate and ownership test downstream must keep seeing
        // the person. The agent claims add who is driving, they do not change who is acting.
        return new ClaimsIdentity(
            [
                new Claim(ClaimDefinitions.sub.Name, validation.UserId.ToString()),
                new Claim(ClaimDefinitions.agent.Name, "true"),
                new Claim(ClaimDefinitions.agentLabel.Name, validation.Label),
                new Claim(
                    ClaimDefinitions.agentKeyId.Name,
                    validation.AgentKeyId.ToString(CultureInfo.InvariantCulture)),
            ],
            AuthenticationSchemes.AgentKey);
    }

    /// <summary>
    /// Validates a personal access token and builds the identity it authenticates.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <param name="rawToken">The presented token.</param>
    /// <returns>The identity, or <see langword="null"/> when the token is not accepted.</returns>
    private async Task<ClaimsIdentity?> AuthenticatePat(HttpContext context, string rawToken)
    {
        WebMcpApiKeyMiddlewareLog.PatHeaderDetected(_logger);

        var service = context.RequestServices.GetService<IPersonalAccessTokenService>();

        if (service is null)
        {
            WebMcpApiKeyMiddlewareLog.PatServiceNotRegistered(_logger);
            return null;
        }

        var result = await service.ValidateToken(rawToken, context.RequestAborted).ConfigureAwait(false);

        if (result.IsFailure)
        {
            WebMcpApiKeyMiddlewareLog.PatValidationError(
                _logger,
                result.CurrentMessage ?? "Validation service error");
            return null;
        }

        if (result.Value is not { IsValid: true } validation)
        {
            WebMcpApiKeyMiddlewareLog.PatAuthenticationFailed(_logger);
            return null;
        }

        WebMcpApiKeyMiddlewareLog.PatAuthenticationSucceeded(_logger, validation.UserId.ToString());

        return new ClaimsIdentity(
            [new Claim(ClaimDefinitions.sub.Name, validation.UserId.ToString())],
            AuthenticationSchemes.PatBearer);
    }
}
