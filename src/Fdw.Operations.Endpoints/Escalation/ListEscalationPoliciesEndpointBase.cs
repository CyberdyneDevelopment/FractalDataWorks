using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Escalation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Operations.Endpoints.Escalation;

/// <summary>
/// Abstract endpoint that lists all escalation policies.
/// </summary>
public abstract class ListEscalationPoliciesEndpointBase : EndpointWithoutRequest<IReadOnlyList<EscalationPolicyResponse>>
{
    private readonly IEscalationService _escalationService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListEscalationPoliciesEndpointBase"/> class.
    /// </summary>
    /// <param name="escalationService">The escalation service.</param>
    /// <param name="logger">The logger instance.</param>
    protected ListEscalationPoliciesEndpointBase(
        IEscalationService escalationService,
        ILogger<ListEscalationPoliciesEndpointBase>? logger)
    {
        _escalationService = escalationService;
        _logger = logger ?? NullLogger<ListEscalationPoliciesEndpointBase>.Instance;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/escalation/policies");
        Policies("datasets:write");
        Summary(s => s.Summary = "List escalation policies");
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or additional endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <summary>Lists all escalation policies.</summary>
    public override async Task HandleAsync(CancellationToken ct)
    {
        OperationsEndpointLog.ListingEscalationPolicies(_logger);

        try
        {
            var result = await _escalationService.GetAllPolicies(ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                OperationsEndpointLog.ListEscalationPoliciesFailed(_logger, result.CurrentMessage!);
                AddError("Failed to list escalation policies");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            var policies = (result.Value ?? Array.Empty<IEscalationPolicy>())
                .Select(EscalationMapper.ToDto)
                .ToList();

            OperationsEndpointLog.EscalationPoliciesFound(_logger, policies.Count);
            await Send.OkAsync(policies, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            OperationsEndpointLog.ListEscalationPoliciesFailed(_logger, ex.Message);
            AddError("Failed to list escalation policies");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
        }
    }
}
