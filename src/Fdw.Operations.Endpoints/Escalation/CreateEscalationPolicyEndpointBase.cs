using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Escalation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Operations.Endpoints.Escalation;

/// <summary>
/// Abstract endpoint that creates an escalation policy.
/// </summary>
public abstract class CreateEscalationPolicyEndpointBase : Endpoint<EscalationPolicyRequest, EscalationPolicyResponse>
{
    private readonly IEscalationService _escalationService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateEscalationPolicyEndpointBase"/> class.
    /// </summary>
    /// <param name="escalationService">The escalation service.</param>
    /// <param name="logger">The logger instance.</param>
    protected CreateEscalationPolicyEndpointBase(
        IEscalationService escalationService,
        ILogger<CreateEscalationPolicyEndpointBase>? logger)
    {
        _escalationService = escalationService;
        _logger = logger ?? NullLogger<CreateEscalationPolicyEndpointBase>.Instance;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Post("/escalation/policies");
        Policies("datasets:write");
        Summary(s => s.Summary = "Create an escalation policy");
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or additional endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <summary>Creates a new escalation policy.</summary>
    public override async Task HandleAsync(EscalationPolicyRequest req, CancellationToken ct)
    {
        OperationsEndpointLog.CreatingEscalationPolicy(_logger, req.Name);

        try
        {
            var model = EscalationMapper.ToModel(req);
            var result = await _escalationService.CreatePolicy(model, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                OperationsEndpointLog.CreateEscalationPolicyFailed(_logger, result.CurrentMessage!);
                AddError("Failed to create escalation policy");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            if (result.Value is null)
            {
                OperationsEndpointLog.CreateEscalationPolicyFailed(_logger, "Service returned null");
                AddError("Failed to create escalation policy");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            OperationsEndpointLog.EscalationPolicyCreated(_logger, result.Value.Name);
            await SendCreatedAtResponse(result.Value, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            OperationsEndpointLog.CreateEscalationPolicyFailed(_logger, ex.Message);
            AddError("Failed to create escalation policy");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
        }
    }

    /// <summary>Sends the created-at response. Override to customize the response location.</summary>
    protected virtual Task SendCreatedAtResponse(IEscalationPolicy policy, CancellationToken ct)
    {
        return Send.CreatedAtAsync<GetEscalationPolicyEndpointBase>(
            new { policy.Id },
            EscalationMapper.ToDto(policy),
            cancellation: ct);
    }
}
