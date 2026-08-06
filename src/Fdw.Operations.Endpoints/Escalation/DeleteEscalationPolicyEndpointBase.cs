using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Escalation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Operations.Endpoints.Escalation;

/// <summary>
/// Abstract endpoint that deletes an escalation policy.
/// </summary>
public abstract class DeleteEscalationPolicyEndpointBase : Endpoint<EscalationPolicyIdRequest>
{
    private readonly IEscalationService _escalationService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteEscalationPolicyEndpointBase"/> class.
    /// </summary>
    /// <param name="escalationService">The escalation service.</param>
    /// <param name="logger">The logger instance.</param>
    protected DeleteEscalationPolicyEndpointBase(
        IEscalationService escalationService,
        ILogger<DeleteEscalationPolicyEndpointBase>? logger)
    {
        _escalationService = escalationService;
        _logger = logger ?? NullLogger<DeleteEscalationPolicyEndpointBase>.Instance;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Delete("/escalation/policies/{Id}");
        Policies("datasets:write");
        Summary(s => s.Summary = "Delete an escalation policy");
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or additional endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <summary>Deletes an escalation policy by its identifier.</summary>
    public override async Task HandleAsync(EscalationPolicyIdRequest req, CancellationToken ct)
    {
        OperationsEndpointLog.DeletingEscalationPolicy(_logger, req.Id);

        try
        {
            var result = await _escalationService.DeletePolicy(req.Id, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                OperationsEndpointLog.DeleteEscalationPolicyFailed(_logger, req.Id, result.CurrentMessage!);
                AddError("Failed to delete escalation policy");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            OperationsEndpointLog.EscalationPolicyDeleted(_logger, req.Id);
            await Send.NoContentAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            OperationsEndpointLog.DeleteEscalationPolicyFailed(_logger, req.Id, ex.Message);
            AddError("Failed to delete escalation policy");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
        }
    }
}
