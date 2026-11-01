using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Endpoints;

/// <summary>
/// What every route that changes a sign-in flow shares: the policy, the service, and how an answer is sent.
/// </summary>
/// <typeparam name="TRequest">The route's request.</typeparam>
/// <remarks>
/// Flows are the host's configuration rather than a tenant's, so changing one needs
/// <c>settings/server:write</c>. The change is made by <see cref="IAuthenticationFlowAdministration"/>,
/// which enforces it on this host at once.
/// </remarks>
public abstract class AuthenticationFlowChangeEndpointBase<TRequest> : Endpoint<TRequest, AuthenticationFlowResponse>
    where TRequest : AuthenticationFlowRouteRequest, new()
{
    /// <summary>Initializes a new instance of the <see cref="AuthenticationFlowChangeEndpointBase{TRequest}"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="administration">Makes the change.</param>
    protected AuthenticationFlowChangeEndpointBase(ILogger logger, IAuthenticationFlowAdministration administration)
    {
        EndpointLogger = logger;
        Administration = administration;
    }

    /// <summary>Gets the logger.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Gets the service that makes the change.</summary>
    protected IAuthenticationFlowAdministration Administration { get; }

    /// <summary>Gets the RBAC policy required by this endpoint. Defaults to "settings/server:write".</summary>
    protected virtual string WritePolicy => "settings/server:write";

    /// <summary>Override to add tags or further configuration.</summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <summary>Applies the route's policy and lets a subclass add to it; call from <c>Configure</c> after the verb.</summary>
    protected void ConfigureChange()
    {
        Policies(WritePolicy);
        ConfigureEndpoint();
    }

    /// <summary>
    /// Makes a change as the calling administrator and answers with the flow as it now stands, or with why
    /// the change was refused.
    /// </summary>
    /// <param name="flowName">The flow, for the log.</param>
    /// <param name="change">The change, given who is making it.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <remarks>
    /// The administrator is the token's subject, with the methods its sign-in proved (<c>amr</c>). The
    /// service needs both to refuse a change that would lock this administrator out. A token naming no
    /// user is refused with 403: nobody would be answerable for the change.
    /// </remarks>
    protected async Task SendChange(
        string flowName,
        Func<AuthenticationFlowAdministrator, Task<IGenericResult<AuthenticationFlowState>>> change,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(change);

        if (!EndpointUserResolver.TryResolveUserId(User, out var userId))
        {
            AuthenticationEndpointLog.FlowChangeCallerUnknown(EndpointLogger, flowName);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        var result = await change(new AuthenticationFlowAdministrator
        {
            UserId = userId,
            AuthenticationMethods = [.. User.FindAll("amr").Select(c => c.Value)],
        }).ConfigureAwait(false);

        if (result.IsSuccess && result.Value is not null)
        {
            await Send.OkAsync(AuthenticationFlowResponse.From(result.Value), ct).ConfigureAwait(false);
            return;
        }

        if (AuthenticationFlowFailure.IsNotFound(result))
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var (statusCode, problem) = ResultHttpStatusMapper.Map(result, HttpContext);
        await Send.ResultAsync(TypedResults.Json(problem, statusCode: statusCode)).ConfigureAwait(false);
    }
}
