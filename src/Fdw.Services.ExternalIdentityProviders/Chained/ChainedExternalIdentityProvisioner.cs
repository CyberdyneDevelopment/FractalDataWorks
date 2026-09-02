using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Fdw.Configuration;

namespace Fdw.Services.ExternalIdentityProviders.Chained;

/// <summary>
/// Composite <see cref="IExternalIdentityProvisioner"/> that walks its ordered
/// <see cref="ChainedExternalIdentityProvisionerConfiguration.Steps"/> children, resolving each named
/// sibling provisioner through the injected <c>IPlatformServiceProvider</c> — NEVER a switch on provisioner
/// type — and delegating <c>Provision</c> to it in turn. The chain does not appear on any
/// storage-format switch above the connection layer: adding a new leaf provisioner is purely a new
/// <c>sec.ChainedProvisionerStep</c> row, never a code change here.
/// </summary>
/// <remarks>
/// <para>
/// Algorithm (<see cref="Provision"/>): sort <see cref="ChainedExternalIdentityProvisionerConfiguration.Steps"/>
/// by <c>ExecutionOrder</c> ascending in memory — the read cascade
/// (<c>ImplementationConfigurationProviderBase.ComposeChildren</c>) does not order children. For each step:
/// </para>
/// <list type="number">
///   <item><description>Resolve the sibling provisioner by name. A resolution failure propagates immediately (hard error).</description></item>
///   <item><description>If the resolved sibling's own <c>ServiceType</c> is <c>"Chained"</c>, log an error for that step and fall through to the next step WITHOUT recursing (nested Chained provisioners are rejected).</description></item>
///   <item><description>Otherwise call the sibling's <c>Provision</c>. Success returns immediately (short-circuit match).</description></item>
///   <item><description>A failure whose <c>Code.Id</c> is the canonical NotFound number (30000 — see <see cref="IExternalIdentityProvisioner"/>'s NOT-FOUND CONTRACT) logs a fall-through trace and continues to the next step.</description></item>
///   <item><description>Any OTHER failure propagates immediately — never masked as a fall-through.</description></item>
/// </list>
/// <para>
/// Full fall-through (every step exhausted, or no steps configured — the current state with no leaf
/// provisioner shipped) returns the last NotFound-coded step failure, or a plain "chain exhausted"
/// failure when no step ever produced one (e.g. zero steps). This is byte-identical to today's
/// default-OFF issuance behavior.
/// </para>
/// </remarks>
internal sealed class ChainedExternalIdentityProvisioner : IExternalIdentityProvisioner
{
    private const int CanonicalNotFoundId = 30000;
    private readonly ChainedExternalIdentityProvisionerConfiguration _typed;
    private readonly IPlatformServiceProvider<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration> _provisionerProvider;
    private readonly ILogger<ChainedExternalIdentityProvisioner> _logger;

    /// <summary>Initializes a new instance of the <see cref="ChainedExternalIdentityProvisioner"/> class.</summary>
    public ChainedExternalIdentityProvisioner(
        ChainedExternalIdentityProvisionerConfiguration typed,
        IPlatformServiceProvider<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration> provisionerProvider,
        ILogger<ChainedExternalIdentityProvisioner>? logger)
    {
        _typed = typed ?? throw new ArgumentNullException(nameof(typed));
        _provisionerProvider = provisionerProvider ?? throw new ArgumentNullException(nameof(provisionerProvider));
        _logger = logger ?? NullLogger<ChainedExternalIdentityProvisioner>.Instance;
    }

    // ── IGenericService ────────────────────────────────────────────────────────────

    /// <inheritdoc cref="IPlatformService.Id" />
    public string Id => _typed.Id.ToString();

    /// <inheritdoc />
    public string Name => _typed.Name;

    /// <inheritdoc cref="IPlatformService.ServiceType" />
    public string ServiceType => "Chained";

    /// <inheritdoc cref="IPlatformService.IsAvailable" />
    public bool IsAvailable => true;

    Task<IGenericResult<T>> IGenericService.Execute<T>(IGenericCommand command, CancellationToken cancellationToken)
        => Task.FromResult(GenericResult<T>.Failure(
            ExternalIdentityProvisionerLog.CommandNotDispatchable(_logger, command?.CommandType ?? "(null)")));

    Task<IGenericResult> IGenericService.Execute(IGenericCommand command, CancellationToken cancellationToken)
        => Task.FromResult<IGenericResult>(GenericResult.Failure(
            ExternalIdentityProvisionerLog.CommandNotDispatchable(_logger, command?.CommandType ?? "(null)")));

    // ── IExternalIdentityProvisioner ────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<IGenericResult<Guid>> Provision(
        string provider,
        string externalSubject,
        ClaimsPrincipal externalPrincipal,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(provider);
        ArgumentException.ThrowIfNullOrEmpty(externalSubject);
        ArgumentNullException.ThrowIfNull(externalPrincipal);

        var steps = _typed.Steps
            .OrderBy(s => s.ExecutionOrder)
            .ToList();

        ExternalIdentityProvisionerLog.ChainStarted(_logger, provider, externalSubject, steps.Count);

        IGenericResult<Guid>? lastNotFound = null;

        foreach (var step in steps)
        {
            var stepResult = await TryStep(step, provider, externalSubject, externalPrincipal, cancellationToken).ConfigureAwait(false);
            if (stepResult is null)
                continue; // Why: nested-Chained rejection — logged inside TryStep, fall through.

            if (stepResult.IsSuccess)
                return stepResult;

            if (stepResult.Code?.Id == CanonicalNotFoundId)
            {
                ExternalIdentityProvisionerLog.StepNotFoundFallThrough(_logger, step.ExecutionOrder, step.ProvisionerName);
                lastNotFound = stepResult;
                continue;
            }

            return stepResult;
        }

        ExternalIdentityProvisionerLog.ChainExhausted(_logger, provider, externalSubject, steps.Count);
        return lastNotFound ?? GenericResult<Guid>.Failure(
            ExternalIdentityProvisionerLog.ChainExhausted(_logger, provider, externalSubject, steps.Count));
    }

    private async Task<IGenericResult<Guid>?> TryStep(
        ChainedProvisionerStepConfiguration step,
        string provider,
        string externalSubject,
        ClaimsPrincipal externalPrincipal,
        CancellationToken cancellationToken)
    {
        ExternalIdentityProvisionerLog.StepAttempting(_logger, step.ExecutionOrder, step.ProvisionerName);

        var resolved = await _provisionerProvider.Get(step.ProvisionerName, cancellationToken).ConfigureAwait(false);
        if (!resolved.IsSuccess || resolved.Value is null)
            return GenericResult<Guid>.Failure(
                ExternalIdentityProvisionerLog.StepResolutionFailed(
                    _logger, step.ExecutionOrder, step.ProvisionerName,
                    resolved.CurrentMessage ?? "provisioner could not be resolved."));

        if (string.Equals(resolved.Value.ServiceType, "Chained", StringComparison.Ordinal))
        {
            ExternalIdentityProvisionerLog.StepNestedChainedRejected(_logger, step.ExecutionOrder, step.ProvisionerName);
            return null;
        }

        var stepResult = await resolved.Value
            .Provision(provider, externalSubject, externalPrincipal, cancellationToken)
            .ConfigureAwait(false);

        if (stepResult.IsSuccess)
            ExternalIdentityProvisionerLog.StepMatched(_logger, step.ExecutionOrder, step.ProvisionerName, stepResult.Value);

        return stepResult;
    }
}
