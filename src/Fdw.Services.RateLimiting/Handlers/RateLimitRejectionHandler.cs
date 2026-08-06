using System;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Fdw.Services.RateLimiting.Logging;

namespace Fdw.Services.RateLimiting.Handlers;

/// <summary>
/// Handler for rate limit rejections that returns HTTP 429 responses with Retry-After headers.
/// </summary>
/// <remarks>
/// <para>
/// This handler is invoked when a request is rejected by the rate limiting middleware.
/// It sets the appropriate HTTP status code (429 Too Many Requests), adds the Retry-After
/// header to indicate when the client can retry, and returns a JSON response body.
/// </para>
/// <para>
/// The Retry-After value is determined from the lease metadata if available, otherwise
/// defaults to 60 seconds.
/// </para>
/// </remarks>
public static class RateLimitRejectionHandler
{
    /// <summary>
    /// Default retry-after duration in seconds when metadata is not available.
    /// </summary>
    private const int DefaultRetryAfterSeconds = 60;

    /// <summary>
    /// Handles a rate limit rejection by returning a 429 response with Retry-After header.
    /// </summary>
    /// <param name="context">The rejection context containing the HTTP context and lease information.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// This method:
    /// </para>
    /// <list type="number">
    /// <item>Sets the HTTP status code to 429 (Too Many Requests)</item>
    /// <item>Calculates the Retry-After value from lease metadata or uses default</item>
    /// <item>Sets the Retry-After header</item>
    /// <item>Logs the rejection via MessageLogging</item>
    /// <item>Writes a JSON response body with error details</item>
    /// </list>
    /// </remarks>
    public static async ValueTask HandleRejection(
        OnRejectedContext context,
        CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;
        var logger = httpContext.RequestServices.GetService<ILogger<RateLimiterOptions>>();
        var clientId = GetClientIdentifier(httpContext);

        // Set 429 Too Many Requests status
        httpContext.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;

        // Calculate retry-after from lease metadata or use default
        var retryAfterSeconds = GetRetryAfterSeconds(context);
        httpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);

        // Log the rejection via MessageLogging
        if (logger != null)
        {
            RateLimitLog.RequestRejected(logger, clientId);
            RateLimitLog.RetryAfterSet(logger, retryAfterSeconds, clientId);
        }

        // Write JSON response body
        await httpContext.Response.WriteAsJsonAsync(
            new RateLimitRejectionResponse
            {
                Error = "Too Many Requests",
                Message = "Rate limit exceeded. Please retry after the specified time.",
                RetryAfterSeconds = retryAfterSeconds
            },
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a client identifier from the HTTP context for logging purposes.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>
    /// The client identifier, preferring X-Forwarded-For header for proxied requests,
    /// falling back to the remote IP address, or "unknown" if neither is available.
    /// </returns>
    private static string GetClientIdentifier(HttpContext context)
    {
        // Try to get client IP from X-Forwarded-For header (for proxied requests)
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var headerValue = forwardedFor.ToString();
            if (!string.IsNullOrEmpty(headerValue))
            {
                // X-Forwarded-For can contain multiple IPs, take the first one (original client)
                var firstIp = headerValue.Split(',')[0].Trim();
                if (!string.IsNullOrEmpty(firstIp))
                {
                    return firstIp;
                }
            }
        }

        // Fall back to the connection's remote IP address
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Gets the retry-after duration in seconds from the lease metadata.
    /// </summary>
    /// <param name="context">The rejection context containing the lease.</param>
    /// <returns>
    /// The retry-after duration in seconds from the lease metadata,
    /// or the default value (60 seconds) if metadata is not available.
    /// </returns>
    private static int GetRetryAfterSeconds(OnRejectedContext context)
    {
        // Try to get retry-after from lease metadata
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            var seconds = (int)retryAfter.TotalSeconds;
            // Ensure at least 1 second to avoid immediate retry floods
            return Math.Max(1, seconds);
        }

        return DefaultRetryAfterSeconds;
    }
}