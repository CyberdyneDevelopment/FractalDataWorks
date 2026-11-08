using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Escalation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Results;

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

        // The policy's choices and each level's severity and recipient type have no default; a request that leaves one out is refused (FDW-3823).
        var missing = EscalationMapper.Missing(req);
        if (missing.Count > 0 || req.IsEnabled is not { } isEnabled || req.MaxEscalationLevel is not { } maxLevel || req.CooldownMinutes is not { } cooldown
            || req.MaxRetries is not { } maxRetries || req.RetryDelaySeconds is not { } retryDelay)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(OperationsEndpointLog.CreateEscalationPolicyFieldsNotStated(_logger, string.Join(", ", missing))), HttpContext)).ConfigureAwait(false);
            return;
        }

        try
        {
            var model = EscalationMapper.ToModel(req, isEnabled, maxLevel, cooldown, maxRetries, retryDelay);
            var result = await _escalationService.CreatePolicy(model, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                OperationsEndpointLog.CreateEscalationPolicyFailed(_logger, result.CurrentMessage!);
                await Send.ResultAsync(ResultHttpStatusMapper.Answer(result, HttpContext)).ConfigureAwait(false);
                return;
            }

            if (result.Value is null)
            {
                await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(OperationsEndpointLog.CreateEscalationPolicyReturnedNothing(_logger)), HttpContext)).ConfigureAwait(false);
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
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(OperationsEndpointLog.CreateEscalationPolicyFaulted(_logger)), HttpContext)).ConfigureAwait(false);
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
