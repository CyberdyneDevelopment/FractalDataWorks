using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.RestEndpoints.Security;

/// <summary>
/// Answers a call that ASP.NET's authorization middleware refused with the same problem body every other refusal sends (FDW-2271).
/// </summary>
/// <remarks>
/// The middleware runs before the endpoint's pre-processors, so a policy it refuses never reaches <see cref="PermissionClaimsPreProcessor"/>; its own
/// handler answered through the authentication handler's <c>ForbidAsync</c>, which set the status and wrote nothing. This handler takes the forbidden
/// outcome and answers it through <see cref="PermissionRefusal"/>, so the caller gets the request's reference and the permission needed and the journal
/// carries the same reference. Every other outcome, and a refusal by an endpoint that states no policy name, goes to ASP.NET's own handler unchanged.
/// </remarks>
public sealed class ProblemAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _aspNetHandler = new();
    private readonly ILogger<ProblemAuthorizationResultHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="ProblemAuthorizationResultHandler"/> class.</summary>
    /// <param name="logger">The logger the refusal is written with.</param>
    public ProblemAuthorizationResultHandler(ILogger<ProblemAuthorizationResultHandler> logger)
    {
        _logger = logger ?? NullLogger<ProblemAuthorizationResultHandler>.Instance;
    }

    /// <inheritdoc />
    public Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (!authorizeResult.Forbidden
            || context.GetEndpoint()?.Metadata.OfType<IAuthorizeData>().FirstOrDefault()?.Policy is not { Length: > 0 } policyName)
        {
            return _aspNetHandler.HandleAsync(next, context, policy, authorizeResult);
        }

        var (statusCode, problem) = PermissionRefusal.Of(context, _logger, policyName);
        return TypedResults.Json(problem, statusCode: statusCode).ExecuteAsync(context);
    }
}
