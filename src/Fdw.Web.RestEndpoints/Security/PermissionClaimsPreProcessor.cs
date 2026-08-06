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
        // Why: Skip unauthenticated requests — they will be rejected by the [Authorize]
        // attribute or JWT bearer middleware before reaching endpoint execution.
        if (context.HttpContext.User?.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        // Why: Skip endpoints that carry no policy requirement. Public endpoints,
        // health checks, and other anonymous-access endpoints should not be affected.
        var policyName = GetPolicyName(context.HttpContext);
        if (string.IsNullOrEmpty(policyName))
            return Task.CompletedTask;

        // Why: Parse "resource:action" from the policy name. If the policy name is
        // not in resource:action form (e.g. epPolicy:ClassName or custom ASP.NET Core
        // policies), skip the pre-processor and let the normal policy provider run.
        if (!TryParsePermission(policyName, out var resource, out var action))
            return Task.CompletedTask;

        // Why: When no perm claims are present in the token (legacy token, or auth
        // server without baked perms), skip this check. The existing per-request
        // FdwAuthorizationPolicyProvider + FrameworkPermissionHandler pipeline handles it.
        var permClaims = context.HttpContext.User.FindAll("perm").ToList();
        if (permClaims.Count == 0)
            return Task.CompletedTask;

        var requiredPermission = $"{resource}:{action}";

        // Why: Only resource:action and resource:* are accepted. The *:* super-grant is removed —
        // OpenIddict tokens carry explicit permission claims; no wildcard bypass is needed or safe.
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

    // Why: FastEndpoints stores the endpoint policy names in endpoint metadata via
    // IEndpointConventionBuilder.WithMetadata. Policies("users:read") results in
    // endpoint metadata on the HttpContext.GetEndpoint(). We read the first AuthorizeAttribute
    // or policy-name metadata to extract the required permission.
    private static string? GetPolicyName(HttpContext httpContext)
    {
        var endpoint = httpContext.GetEndpoint();
        if (endpoint is null)
            return null;

        // Why: FastEndpoints registers authorized endpoints via a custom metadata
        // that stores policy names. Fall back to IAuthorizeData for vanilla ASP.NET Core.
        var authorizeData = endpoint.Metadata
            .OfType<Microsoft.AspNetCore.Authorization.IAuthorizeData>()
            .FirstOrDefault();

        return authorizeData?.Policy;
    }

    // Why: Only "resource:action" patterns go through the perm-claims check.
    // epPolicy:ClassName and other framework-internal policy names are passed through.
    private static bool TryParsePermission(string policyName, out string resource, out string action)
    {
        resource = string.Empty;
        action = string.Empty;

        // Why: epPolicy: prefix is FastEndpoints internal; skip it.
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
