using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Agents.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Operations.Endpoints.AgentActions;

/// <summary>
/// Abstract endpoint that denies a pending agent action.
/// </summary>
public abstract class DenyAgentActionEndpointBase : Endpoint<AgentActionIdRequest>
{
    private readonly IAgentActionService _agentActionService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DenyAgentActionEndpointBase"/> class.
    /// </summary>
    /// <param name="agentActionService">The agent action service.</param>
    /// <param name="logger">The logger instance.</param>
    protected DenyAgentActionEndpointBase(
        IAgentActionService agentActionService,
        ILogger<DenyAgentActionEndpointBase>? logger)
    {
        _agentActionService = agentActionService;
        _logger = logger ?? NullLogger<DenyAgentActionEndpointBase>.Instance;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Post("/agent-actions/{ActionId}/deny");
        Policies("agent-actions:manage");
        Summary(s => s.Summary = "Deny a pending agent action");
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or additional endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <summary>Denies a pending agent action using the authenticated user's identity.</summary>
    public override async Task HandleAsync(AgentActionIdRequest req, CancellationToken ct)
    {
        var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            OperationsEndpointLog.AgentActionUserClaimNotFound(_logger);
            await Send.UnauthorizedAsync(ct).ConfigureAwait(false);
            return;
        }

        OperationsEndpointLog.ReviewingAgentAction(_logger, req.ActionId, "Denied");

        try
        {
            var result = await _agentActionService.Deny(req.ActionId, userIdClaim, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                OperationsEndpointLog.ReviewAgentActionFailed(_logger, req.ActionId, "Denied", result.CurrentMessage!);
                AddError(result.CurrentMessage!);
                await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
                return;
            }

            OperationsEndpointLog.AgentActionReviewed(_logger, req.ActionId, "Denied", userIdClaim);
            await Send.NoContentAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            OperationsEndpointLog.ReviewAgentActionFailed(_logger, req.ActionId, "Denied", ex.Message);
            AddError("Failed to deny agent action");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
        }
    }
}
