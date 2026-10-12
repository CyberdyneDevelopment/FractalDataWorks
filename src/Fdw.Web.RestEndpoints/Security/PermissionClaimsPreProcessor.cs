using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Security;
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
///     Checks the token's <c>perm</c> claims by the rule in <see cref="PermissionCoverage"/>.
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

        // The same rule every other check uses, so no grant is refused here and admitted by the policy handler,
        // or the other way round.
        var hasPermission = PermissionCoverage.Covers(permClaims.Select(c => c.Value), requiredPermission);

        if (!hasPermission)
        {
            var userId = context.HttpContext.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                      ?? context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? "(unknown)";
            var logger = context.HttpContext.RequestServices.GetService(typeof(ILogger<PermissionClaimsPreProcessor>)) as ILogger
                      ?? NullLogger.Instance;
            EndpointLogger.PermissionDeniedByPreProcessor(logger, userId, requiredPermission);
            // Through FastEndpoints' sender: that is what marks the response as sent, and a
            // pre-processor's sent response is what stops the handler from running at all.
            return context.HttpContext.Response.SendForbiddenAsync(ct);
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

        // The platform-admin policy is shaped like a permission but is not one: no token carries it as a perm
        // claim, so treating it as one would refuse every caller. The policy provider answers it from the
        // Admin role and the global-tenant claim.
        if (string.Equals(policyName, AuthConstants.PlatformAdminPolicy, StringComparison.Ordinal))
            return false;

        var colonIdx = policyName.IndexOf(':', StringComparison.Ordinal);
        if (colonIdx <= 0 || colonIdx == policyName.Length - 1)
            return false;

        resource = policyName[..colonIdx];
        action = policyName[(colonIdx + 1)..];
        return true;
    }
}
