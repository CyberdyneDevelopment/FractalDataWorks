using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Agents.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Results;

namespace Fdw.Operations.Endpoints.AgentActions;

/// <summary>
/// Abstract endpoint that gets a single agent action by ID.
/// </summary>
public abstract class GetAgentActionEndpointBase : Endpoint<AgentActionIdRequest, AgentActionRecord>
{
    private readonly IAgentActionService _agentActionService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAgentActionEndpointBase"/> class.
    /// </summary>
    /// <param name="agentActionService">The agent action service.</param>
    /// <param name="logger">The logger instance.</param>
    protected GetAgentActionEndpointBase(
        IAgentActionService agentActionService,
        ILogger<GetAgentActionEndpointBase>? logger)
    {
        _agentActionService = agentActionService;
        _logger = logger ?? NullLogger<GetAgentActionEndpointBase>.Instance;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/agent-actions/{ActionId}");
        Policies("agent-actions:read");
        Summary(s => s.Summary = "Get an agent action by ID");
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or additional endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <summary>Gets a single agent action by its identifier.</summary>
    public override async Task HandleAsync(AgentActionIdRequest req, CancellationToken ct)
    {
        OperationsEndpointLog.GettingAgentAction(_logger, req.ActionId);

        try
        {
            var result = await _agentActionService.Get(req.ActionId, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(OperationsEndpointLog.AgentActionNotFound(_logger, req.ActionId)), HttpContext)).ConfigureAwait(false);
                return;
            }

            await Send.OkAsync(result.Value!, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            OperationsEndpointLog.ReviewAgentActionFailed(_logger, req.ActionId, "Get", ex.Message);
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(OperationsEndpointLog.GetAgentActionFaulted(_logger)), HttpContext)).ConfigureAwait(false);
        }
    }
}
