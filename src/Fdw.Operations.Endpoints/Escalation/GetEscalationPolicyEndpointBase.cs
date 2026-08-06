using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Escalation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Operations.Endpoints.Escalation;

/// <summary>
/// Abstract endpoint that gets an escalation policy by ID.
/// </summary>
public abstract class GetEscalationPolicyEndpointBase : Endpoint<EscalationPolicyIdRequest, EscalationPolicyResponse>
{
    private readonly IEscalationService _escalationService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetEscalationPolicyEndpointBase"/> class.
    /// </summary>
    /// <param name="escalationService">The escalation service.</param>
    /// <param name="logger">The logger instance.</param>
    protected GetEscalationPolicyEndpointBase(
        IEscalationService escalationService,
        ILogger<GetEscalationPolicyEndpointBase>? logger)
    {
        _escalationService = escalationService;
        _logger = logger ?? NullLogger<GetEscalationPolicyEndpointBase>.Instance;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/escalation/policies/{Id}");
        Policies("datasets:read");
        Summary(s => s.Summary = "Get an escalation policy");
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or additional endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <summary>Gets an escalation policy by its identifier.</summary>
    public override async Task HandleAsync(EscalationPolicyIdRequest req, CancellationToken ct)
    {
        OperationsEndpointLog.GettingEscalationPolicy(_logger, req.Id);

        try
        {
            var result = await _escalationService.GetPolicy(req.Id, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                OperationsEndpointLog.GetEscalationPolicyFailed(_logger, req.Id, result.CurrentMessage!);
                AddError("Failed to get escalation policy");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            if (result.Value is null)
            {
                OperationsEndpointLog.EscalationPolicyNotFound(_logger, req.Id);
                await Send.NotFoundAsync(ct).ConfigureAwait(false);
                return;
            }

            await Send.OkAsync(EscalationMapper.ToDto(result.Value), ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            OperationsEndpointLog.GetEscalationPolicyFailed(_logger, req.Id, ex.Message);
            AddError("Failed to get escalation policy");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
        }
    }
}
