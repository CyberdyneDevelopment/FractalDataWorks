using System.Security.Claims;
using Fdw.Results;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Web.RestEndpoints.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.RestEndpoints.Security;

/// <summary>
/// Builds the answer to a call refused for a missing permission, in one place for every place that refuses one (FDW-2271): the declared message is logged with the
/// request's reference, and the problem body the caller gets carries the same reference and names the permission needed.
/// </summary>
internal static class PermissionRefusal
{
    /// <summary>Logs the refusal and returns the status and problem body to send.</summary>
    /// <param name="httpContext">The refused request.</param>
    /// <param name="logger">The logger of the refusing component.</param>
    /// <param name="requiredPermission">The permission, or the policy name, the endpoint states.</param>
    internal static (int StatusCode, ProblemDetails Problem) Of(HttpContext httpContext, ILogger logger, string requiredPermission) =>
        ResultHttpStatusMapper.Map(
            GenericResult.Failure(EndpointLogger.PermissionDeniedByPreProcessor(
                logger,
                httpContext.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                    ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? "(unknown)",
                requiredPermission,
                httpContext.TraceIdentifier)),
            httpContext);
}
