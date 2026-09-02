using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Flow;

/// <summary>
/// Assembles flows from their rows and checks each one before anybody tries to log in.
/// </summary>
/// <remarks>
/// Caches after the first load. Flows change by <c>UPDATE</c> rather than deployment, so a host
/// picks up a change on restart — deliberate, because a flow changing under a login in progress is a
/// harder thing to reason about than a restart.
/// <para>
/// One flow's failure never blocks another's: every row is validated independently and gets its own
/// cache entry either way. A flow that is seeded but not yet fully wired — a step whose package
/// isn't referenced yet, say — fails loud for callers asking for THAT flow by name, and stays
/// entirely invisible to every other flow's resolution.
/// </para>
/// </remarks>
public sealed class AuthenticationFlowProvider : IAuthenticationFlowProvider
{
    /// <summary>
    /// One row's outcome. <see cref="Flow"/> set means valid and ready to run; unset means
    /// <see cref="InvalidReason"/> says why — never both, never neither. Kept private to this
    /// provider: <see cref="AuthenticationFlow"/> itself carries no validity concept, because every
    /// other holder of one (the runner included) already assumes it is good.
    /// </summary>
    private sealed record CachedFlow(AuthenticationFlow? Flow, string? InvalidReason);

    private readonly ImplementationConfigurationProviderBase<
        AuthenticationFlowConfiguration, AuthenticationFlowConfigurationCommand> _flows;
    private readonly ImplementationConfigurationProviderBase<
        AuthenticationFlowStepConfiguration, AuthenticationFlowStepConfigurationCommand> _steps;
    private readonly ConcurrentDictionary<string, CachedFlow> _cache = new(StringComparer.Ordinal);
    private readonly ILogger<AuthenticationFlowProvider> _logger;

    /// <summary>Initializes a new instance of the <see cref="AuthenticationFlowProvider"/> class.</summary>
    /// <param name="flows">Reads the flow rows.</param>
    /// <param name="steps">Reads their step rows.</param>
    /// <param name="logger">The logger.</param>
    public AuthenticationFlowProvider(
        ImplementationConfigurationProviderBase<AuthenticationFlowConfiguration, AuthenticationFlowConfigurationCommand> flows,
        ImplementationConfigurationProviderBase<AuthenticationFlowStepConfiguration, AuthenticationFlowStepConfigurationCommand> steps,
        ILogger<AuthenticationFlowProvider>? logger = null)
    {
        _flows = flows ?? throw new ArgumentNullException(nameof(flows));
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _logger = logger ?? NullLogger<AuthenticationFlowProvider>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<AuthenticationFlow>> Get(
        string flowName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(flowName))
            return GenericResult<AuthenticationFlow>.Failure(FlowProviderLog.NameMissing(_logger));

        if (_cache.TryGetValue(flowName, out var cached))
            return Resolve(flowName, cached);

        var loaded = await LoadAndValidate(cancellationToken).ConfigureAwait(false);
        if (loaded.IsFailure)
            return loaded.ToNewResult<AuthenticationFlow>();

        return _cache.TryGetValue(flowName, out var flow)
            ? Resolve(flowName, flow)
            : GenericResult<AuthenticationFlow>.Failure(FlowProviderLog.NoSuchFlow(
                _logger, flowName, string.Join(", ", ValidNames())));
    }

    // I5 — a flow that failed validation is a known, named failure, not an unknown selection: the
    // two produce different messages so a caller (and whoever reads the log) can tell "you typed the
    // wrong name" apart from "this flow is configured and broken, see why it failed above".
    private IGenericResult<AuthenticationFlow> Resolve(string flowName, CachedFlow cached)
    {
        if (cached.Flow is { } flow)
        {
            FlowProviderLog.FlowServed(_logger, flowName);
            return GenericResult<AuthenticationFlow>.Success(flow);
        }

        return GenericResult<AuthenticationFlow>.Failure(
            FlowProviderLog.FlowKnownInvalid(_logger, flowName, cached.InvalidReason ?? "unknown reason"));
    }

    // Only the flows that are actually usable — naming a broken one back to the caller as a
    // "configured" alternative would just move the same failure to whatever they tried next.
    private IEnumerable<string> ValidNames()
        => _cache.Where(kv => kv.Value.Flow is not null)
                 .Select(kv => kv.Key)
                 .OrderBy(k => k, StringComparer.Ordinal);

    /// <inheritdoc />
    public async Task<IGenericResult> LoadAndValidate(CancellationToken cancellationToken = default)
    {
        var rows = await _flows.Get(cancellationToken).ConfigureAwait(false);
        if (rows.IsFailure)
            return GenericResult.Failure(FlowProviderLog.RowsUnreadable(_logger, "auth.AuthenticationFlow"));

        var stepRows = await _steps.Get(cancellationToken).ConfigureAwait(false);
        if (stepRows.IsFailure)
            return GenericResult.Failure(FlowProviderLog.RowsUnreadable(_logger, "auth.AuthenticationFlowStep"));

        foreach (var row in rows.Value ?? [])
        {
            var ordered = (stepRows.Value ?? [])
                .Where(s => s.AuthenticationFlowId == row.Id)
                .OrderBy(s => s.StepOrder)
                .ToList();

            if (ordered.Count == 0)
            {
                FlowProviderLog.FlowHasNoSteps(_logger, row.Name);
                _cache[row.Name] = new CachedFlow(null, "has no steps");
                continue;
            }

            var flow = new AuthenticationFlow
            {
                Name = row.Name,
                Steps = [.. ordered.Select(s => s.StepName)],
                Audience = row.Audience,
                MinimumAcr = row.MinimumAcr,
                ExecutionLifetime = row.ExecutionLifetime,
            };

            var valid = Validate(flow);
            if (valid.IsFailure)
            {
                // Why: Validate() already logged the specific reason (StepNotAvailable/OrderInvalid)
                // as a side effect of building this failure. Stashing CurrentMessage here is so
                // Get() can report it again to whoever asked for THIS flow by name, without this
                // row's problem stopping any other row from loading.
                _cache[row.Name] = new CachedFlow(null, valid.CurrentMessage ?? "failed validation");
                continue;
            }

            _cache[row.Name] = new CachedFlow(flow, null);
            FlowProviderLog.FlowAssembled(_logger, row.Name, ordered.Count);
        }

        FlowProviderLog.FlowsLoaded(_logger, ValidNames().Count(), _cache.Count);
        return GenericResult.Success();
    }

    /// <summary>Checks that every step a flow names exists and that the order satisfies each one.</summary>
    /// <param name="flow">The flow to check.</param>
    /// <remarks>
    /// Run when a flow's row loads, so a flow naming a step whose package is absent — or ordering two
    /// steps wrongly — fails with the missing thing named the first time that flow is asked for,
    /// rather than at 3am on someone's login. The collection answers by name and what it returns IS
    /// the step, so its declarations are read straight off it. A failure here is this flow's alone:
    /// the caller stashes the reason and moves on to the next row.
    /// </remarks>
    private IGenericResult Validate(AuthenticationFlow flow)
    {
        var established = new HashSet<IContextElement>();

        foreach (var stepName in flow.Steps)
        {
            if (AuthenticationStepTypes.ByName(stepName) is not IAuthenticationStep step)
                return GenericResult.Failure(FlowProviderLog.StepNotAvailable(
                    _logger, flow.Name, stepName,
                    string.Join(", ", AuthenticationStepTypes.All().Values.Select(o => o.Name))));

            var missing = step.Requires.Where(r => !established.Contains(r)).ToList();
            if (missing.Count > 0)
                return GenericResult.Failure(FlowProviderLog.OrderInvalid(
                    _logger, flow.Name, stepName, string.Join(", ", missing)));

            foreach (var contributed in step.Contributes)
                established.Add(contributed);
        }

        return GenericResult.Success();
    }
}
