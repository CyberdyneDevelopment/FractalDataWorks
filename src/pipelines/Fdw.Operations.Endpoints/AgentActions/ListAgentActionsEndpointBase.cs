using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Conventions;
using Fdw.Services.Agents.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Results;

namespace Fdw.Operations.Endpoints.AgentActions;

/// <summary>
/// Abstract endpoint that lists agent actions with optional status filter.
/// </summary>
public abstract class ListAgentActionsEndpointBase : Endpoint<ListAgentActionsRequest, IReadOnlyList<AgentActionRecord>>
{
    private readonly IAgentActionService _agentActionService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListAgentActionsEndpointBase"/> class.
    /// </summary>
    /// <param name="agentActionService">The agent action service.</param>
    /// <param name="logger">The logger instance.</param>
    protected ListAgentActionsEndpointBase(
        IAgentActionService agentActionService,
        ILogger<ListAgentActionsEndpointBase>? logger)
    {
        _agentActionService = agentActionService;
        _logger = logger ?? NullLogger<ListAgentActionsEndpointBase>.Instance;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/agent-actions");
        Policies("agent-actions:read");
        Summary(s => s.Summary = "List agent actions pending review");
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or additional endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <summary>Lists agent actions, optionally filtered by status.</summary>
    public override async Task HandleAsync(ListAgentActionsRequest req, CancellationToken ct)
    {
        var statusFilter = req.Status ?? LogPlaceholders.NoFilter;
        OperationsEndpointLog.ListingAgentActions(_logger, statusFilter);

        try
        {
            var result = await _agentActionService.List(req.Status, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                OperationsEndpointLog.ListAgentActionsFailed(_logger, result.CurrentMessage!);
                await Send.ResultAsync(ResultHttpStatusMapper.Answer(result, HttpContext)).ConfigureAwait(false);
                return;
            }

            OperationsEndpointLog.AgentActionsListed(_logger, result.Value!.Count);
            await Send.OkAsync(result.Value!, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            OperationsEndpointLog.ListAgentActionsFailed(_logger, ex.Message);
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(OperationsEndpointLog.ListAgentActionsFaulted(_logger)), HttpContext)).ConfigureAwait(false);
        }
    }
}
