using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Security;
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
/// When an endpoint carries a <c>Policies("resource:action")</c> declaration (or several joined by a bar, any one of which is enough), this processor:
/// <list type="bullet">
///   <item><description>Reads the required permission from the policy name.</description></item>
///   <item><description>
///     Checks the token's <c>perm</c> claims by the rule in <see cref="PermissionCoverage"/>.
///   </description></item>
///   <item><description>Returns 403 if the required permission is absent, as the problem body every refusal sends, with the request's referenceId and the permission needed.</description></item>
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

        if (!TryParsePermissions(policyName, out var permissions))
            return Task.CompletedTask;

        var permClaims = context.HttpContext.User.FindAll("perm").ToList();
        if (permClaims.Count == 0)
            return Task.CompletedTask;

        var requiredPermission = string.Join(PermissionPolicyName.Alternative, permissions.Select(p => $"{p.Resource}:{p.Action}"));

        // The same rule every other check uses, so no grant is refused here and admitted by the policy handler,
        // or the other way round. A policy that lists several permissions is met by any one of them.
        var held = permClaims.Select(c => c.Value).ToList();
        var hasPermission = permissions.Any(p => PermissionCoverage.Covers(held, $"{p.Resource}:{p.Action}"));

        if (!hasPermission)
        {
            var logger = context.HttpContext.RequestServices.GetService(typeof(ILogger<PermissionClaimsPreProcessor>)) as ILogger
                      ?? NullLogger.Instance;
            // Why the refusal is built in PermissionRefusal: the authorization middleware refuses a policy before this runs, and answers through the same
            // builder (ProblemAuthorizationResultHandler), so a refusal reads the same wherever it is made: the status from the message's number
            // (101000 is band 10, a 403), the request's referenceId, the permission needed, and a journal line carrying the same referenceId.
            var (statusCode, problem) = PermissionRefusal.Of(context.HttpContext, logger, requiredPermission);
            // Through FastEndpoints' sender: that is what marks the response as sent, and a
            // pre-processor's sent response is what stops the handler from running at all.
            return context.HttpContext.Response.SendResultAsync(TypedResults.Json(problem, statusCode: statusCode));
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

    private static bool TryParsePermissions(string policyName, out IReadOnlyList<(string Resource, string Action)> permissions)
    {
        permissions = [];

        if (policyName.StartsWith("epPolicy:", StringComparison.OrdinalIgnoreCase))
            return false;

        // The platform-admin policy is shaped like a permission but is not one: no token carries it as a perm
        // claim, so treating it as one would refuse every caller. The policy provider answers it from the
        // Admin role and the global-tenant claim.
        if (string.Equals(policyName, AuthConstants.PlatformAdminPolicy, StringComparison.Ordinal))
            return false;

        return PermissionPolicyName.TryParse(policyName, out permissions);
    }
}
