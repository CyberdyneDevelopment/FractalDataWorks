using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Escalation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Results;

namespace Fdw.Operations.Endpoints.Escalation;

/// <summary>
/// Abstract endpoint that updates an escalation policy.
/// </summary>
public abstract class UpdateEscalationPolicyEndpointBase : Endpoint<UpdateEscalationPolicyRequest, EscalationPolicyResponse>
{
    private readonly IEscalationService _escalationService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateEscalationPolicyEndpointBase"/> class.
    /// </summary>
    /// <param name="escalationService">The escalation service.</param>
    /// <param name="logger">The logger instance.</param>
    protected UpdateEscalationPolicyEndpointBase(
        IEscalationService escalationService,
        ILogger<UpdateEscalationPolicyEndpointBase>? logger)
    {
        _escalationService = escalationService;
        _logger = logger ?? NullLogger<UpdateEscalationPolicyEndpointBase>.Instance;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Patch("/escalation/policies/{Id}");
        Policies("datasets:write");
        Summary(s => s.Summary = "Update an escalation policy");
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or additional endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <summary>Updates an existing escalation policy.</summary>
    public override async Task HandleAsync(UpdateEscalationPolicyRequest req, CancellationToken ct)
    {
        OperationsEndpointLog.UpdatingEscalationPolicy(_logger, req.Id);

        // Levels a request sends state their severity and recipient type; there is no default (FDW-3823).
        var missing = EscalationMapper.MissingOnLevels(req.Levels);
        if (missing.Count > 0)
        {
            AddError("Required and not stated: " + string.Join(", ", missing));
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        try
        {
            var existing = await _escalationService.GetPolicy(req.Id, ct).ConfigureAwait(false);
            if (existing.IsFailure)
            {
                OperationsEndpointLog.UpdateEscalationPolicyFailed(_logger, req.Id, existing.FailureReason().Message);
                AddError("Failed to update escalation policy");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            // Why its own branch: the service answers a policy it does not hold with a success and no value, which is a not-found.
            if (existing.Value is null)
            {
                OperationsEndpointLog.EscalationPolicyNotFound(_logger, req.Id);
                AddError("Failed to update escalation policy");
                await Send.ErrorsAsync(404, ct).ConfigureAwait(false);
                return;
            }

            var model = EscalationMapper.ToModel(req, existing.Value);
            var result = await _escalationService.UpdatePolicy(req.Id, model, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                OperationsEndpointLog.UpdateEscalationPolicyFailed(_logger, req.Id, result.CurrentMessage!);
                AddError("Failed to update escalation policy");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            if (result.Value is null)
            {
                OperationsEndpointLog.UpdateEscalationPolicyFailed(_logger, req.Id, "Service returned null");
                AddError("Failed to update escalation policy");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            OperationsEndpointLog.EscalationPolicyUpdated(_logger, req.Id);
            await Send.OkAsync(EscalationMapper.ToDto(result.Value), ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            OperationsEndpointLog.UpdateEscalationPolicyFailed(_logger, req.Id, ex.Message);
            AddError("Failed to update escalation policy");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
        }
    }
}
