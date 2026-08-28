using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Web.RestEndpoints.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.RestEndpoints.Security;

/// <summary>
/// Global FastEndpoints pre-processor that authorizes requests against the <c>perm</c> claims
/// baked into the JWT at token-issue time.
/// </summary>
/// <remarks>
/// <para>
/// When an endpoint carries a <c>Policies("resource:action")</c> declaration, this processor:
/// <list type="bullet">
///   <item><description>Reads the required permission from the policy name.</description></item>
///   <item><description>
///     Checks the token's <c>perm</c> claims (incl. <c>resource:*</c> wildcard).
///   </description></item>
///   <item><description>Returns 403 if the required permission is absent.</description></item>
/// </list>
/// </para>
/// <para>
/// When the token carries no <c>perm</c> claims (auth server without baking),
/// this processor skips the check and defers to the existing
/// <c>FdwAuthorizationPolicyProvider</c> + <c>FrameworkPermissionHandler</c> pipeline.
/// </para>
/// </remarks>
public sealed class PermissionClaimsPreProcessor : IGlobalPreProcessor
{
    /// <inheritdoc />
    public Task PreProcessAsync(IPreProcessorContext context, CancellationToken ct)
    {
        if (context.HttpContext.User?.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        var policyName = GetPolicyName(context.HttpContext);
        if (string.IsNullOrEmpty(policyName))
            return Task.CompletedTask;

        if (!TryParsePermission(policyName, out var resource, out var action))
            return Task.CompletedTask;

        var permClaims = context.HttpContext.User.FindAll("perm").ToList();
        if (permClaims.Count == 0)
            return Task.CompletedTask;

        var requiredPermission = $"{resource}:{action}";

        var hasPermission =
            permClaims.Any(c => string.Equals(c.Value, requiredPermission, StringComparison.OrdinalIgnoreCase)) ||
            permClaims.Any(c => string.Equals(c.Value, $"{resource}:*", StringComparison.OrdinalIgnoreCase));

        if (!hasPermission)
        {
            var userId = context.HttpContext.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                      ?? context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? "(unknown)";
            var logger = context.HttpContext.RequestServices.GetService(typeof(ILogger<PermissionClaimsPreProcessor>)) as ILogger
                      ?? NullLogger.Instance;
            EndpointLogger.PermissionDeniedByPreProcessor(logger, userId, requiredPermission);
            context.HttpContext.Response.StatusCode = 403;
            return context.HttpContext.Response.WriteAsync("Forbidden", ct);
        }

        return Task.CompletedTask;
    }

    private static string? GetPolicyName(HttpContext httpContext)
    {
        var endpoint = httpContext.GetEndpoint();
        if (endpoint is null)
            return null;

        var authorizeData = endpoint.Metadata
            .OfType<Microsoft.AspNetCore.Authorization.IAuthorizeData>()
            .FirstOrDefault();

        return authorizeData?.Policy;
    }

    private static bool TryParsePermission(string policyName, out string resource, out string action)
    {
        resource = string.Empty;
        action = string.Empty;

        if (policyName.StartsWith("epPolicy:", StringComparison.OrdinalIgnoreCase))
            return false;

        var colonIdx = policyName.IndexOf(':', StringComparison.Ordinal);
        if (colonIdx <= 0 || colonIdx == policyName.Length - 1)
            return false;

        resource = policyName[..colonIdx];
        action = policyName[(colonIdx + 1)..];
        return true;
    }
}
