using System;
using System.IO;
using System.Net.Mime;
using System.Text.Json;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Fdw.Aui.Logging;

namespace Fdw.Aui;

/// <summary>
/// Middleware that intercepts agent requests and serves the AUI manifest.
/// </summary>
public sealed class AuiMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuiMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuiMiddleware"/> class.
    /// </summary>
    public AuiMiddleware(RequestDelegate next, ILogger<AuiMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    public async Task Invoke(HttpContext context, AuiService auiService)
    {
        var path = context.Request.Path.Value ?? "/";
        
        // Detect if the request is for the AUI facet
        var isAuiRequest = context.Request.Headers["Accept"].ToString().Contains("application/vnd.fdw.aui+json") ||
                           path.EndsWith(".aui", StringComparison.OrdinalIgnoreCase);

        if (isAuiRequest)
        {
            AuiLog.AgentDetected(_logger, path);

            var route = path.Replace(".aui", string.Empty);

            var caller = new ClaimsPrincipalAuthenticationContext(context.User);
            if (!caller.IsAuthenticated || !Guid.TryParse(caller.UserId, out var userId))
            {
                await Answer(GenericResult.Failure(AuiLog.SignInRequired(_logger, path)), context).ConfigureAwait(false);
                return;
            }

            var result = await auiService.GetManifest(userId, route, context.RequestAborted).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(result.Value)).ConfigureAwait(false);
                return;
            }

            await Answer(result, context).ConfigureAwait(false);
            return;
        }

        await _next(context).ConfigureAwait(false);
    }

    private static Task Answer(IGenericResult failure, HttpContext context)
    {
        return ResultHttpStatusMapper.Answer(failure, context).ExecuteAsync(context);
    }
}
