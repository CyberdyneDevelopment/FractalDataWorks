using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Hosting.WebMcp;

/// <summary>
/// Middleware that validates the WebMCP API key header and sets the request principal
/// to the associated user identity with agent claims. Requests without the header
/// pass through unmodified — normal authentication applies.
/// </summary>
internal sealed class WebMcpApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly WebMcpOptions _options;
    private readonly ILogger<WebMcpApiKeyMiddleware> _logger;

    public WebMcpApiKeyMiddleware(
        RequestDelegate next,
        WebMcpOptions options,
        ILogger<WebMcpApiKeyMiddleware>? logger = null)
    {
        _next = next;
        _options = options;
        _logger = logger ?? NullLogger<WebMcpApiKeyMiddleware>.Instance;
    }

    public async Task Invoke(HttpContext context)
    {
        if (_options.AgentKeys.Count == 0)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (!context.Request.Headers.TryGetValue(_options.ApiKeyHeader, out var keyValues))
        {
            // No WebMCP key header — pass through to normal auth
            await _next(context).ConfigureAwait(false);
            return;
        }

        var providedKey = keyValues.ToString();

        var agentKey = _options.AgentKeys
            .FirstOrDefault(k => string.Equals(k.Key, providedKey, StringComparison.Ordinal));

        if (agentKey is null)
        {
            WebMcpLog.AgentKeyRejected(_logger, context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        WebMcpLog.AgentKeyAccepted(_logger, agentKey.Label, agentKey.UserId);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, agentKey.UserId),
            new Claim(ClaimTypes.Name, agentKey.Label),
            new Claim("agent", "true"),
            new Claim("agent_label", agentKey.Label),
        };

        var identity = new ClaimsIdentity(claims, "WebMcpApiKey");
        context.User = new ClaimsPrincipal(identity);

        WebMcpLog.AgentRequestAuthenticated(_logger, agentKey.Label, agentKey.UserId, context.Request.Path);

        await _next(context).ConfigureAwait(false);
    }
}
