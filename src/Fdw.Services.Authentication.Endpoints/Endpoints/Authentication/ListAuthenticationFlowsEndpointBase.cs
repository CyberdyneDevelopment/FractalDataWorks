using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Endpoints;

/// <summary>
/// Generic base endpoint listing every configured sign-in flow and who may use it (GET /auth/flows).
/// </summary>
/// <remarks>
/// Lists switched-off and broken flows too, unlike the anonymous provider list a login screen reads:
/// those are the flows an administrator needs to see. Reading them needs <c>settings/server:read</c>.
/// </remarks>
public abstract class ListAuthenticationFlowsEndpointBase : EndpointWithoutRequest<IReadOnlyList<AuthenticationFlowResponse>>
{
    private readonly IAuthenticationFlowAdministration _administration;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="ListAuthenticationFlowsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="administration">Reads the flows.</param>
    protected ListAuthenticationFlowsEndpointBase(ILogger logger, IAuthenticationFlowAdministration administration)
    {
        _logger = logger;
        _administration = administration;
    }

    /// <summary>Gets the RBAC policy required by this endpoint. Defaults to "settings/server:read".</summary>
    protected virtual string ReadPolicy => "settings/server:read";

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/auth/flows");
        Policies(ReadPolicy);
        Summary(s =>
        {
            s.Summary = "List sign-in flows";
            s.Description = "Every configured sign-in flow: whether it is switched on and served, the methods it proves, and the permission a user must hold to use it.";
        });
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or further configuration.</summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        var flows = await _administration.Get(ct).ConfigureAwait(false);
        if (flows.IsFailure || flows.Value is null)
        {
            var (statusCode, problem) = ResultHttpStatusMapper.Map(flows, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: statusCode)).ConfigureAwait(false);
            return;
        }

        AuthenticationEndpointLog.FlowsListed(_logger, flows.Value.Count);
        await Send.OkAsync([.. flows.Value.Select(AuthenticationFlowResponse.From)], ct).ConfigureAwait(false);
    }
}
