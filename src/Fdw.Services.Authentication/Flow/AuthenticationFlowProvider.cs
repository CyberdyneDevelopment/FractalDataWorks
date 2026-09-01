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
/// </remarks>
public sealed class AuthenticationFlowProvider : IAuthenticationFlowProvider
{
    private readonly ImplementationConfigurationProviderBase<
        AuthenticationFlowConfiguration, AuthenticationFlowConfigurationCommand> _flows;
    private readonly ImplementationConfigurationProviderBase<
        AuthenticationFlowStepConfiguration, AuthenticationFlowStepConfigurationCommand> _steps;
    private readonly ConcurrentDictionary<string, AuthenticationFlow> _cache = new(StringComparer.Ordinal);
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
        {
            FlowProviderLog.FlowServed(_logger, flowName);
            return GenericResult<AuthenticationFlow>.Success(cached);
        }

        var loaded = await LoadAndValidate(cancellationToken).ConfigureAwait(false);
        if (loaded.IsFailure)
            return loaded.ToNewResult<AuthenticationFlow>();

        return _cache.TryGetValue(flowName, out var flow)
            ? GenericResult<AuthenticationFlow>.Success(flow)
            : GenericResult<AuthenticationFlow>.Failure(FlowProviderLog.NoSuchFlow(
                _logger, flowName, string.Join(", ", _cache.Keys.OrderBy(k => k, StringComparer.Ordinal))));
    }

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
                return GenericResult.Failure(FlowProviderLog.FlowHasNoSteps(_logger, row.Name));

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
                return valid;

            _cache[row.Name] = flow;
            FlowProviderLog.FlowAssembled(_logger, row.Name, ordered.Count);
        }

        FlowProviderLog.FlowsLoaded(_logger, _cache.Count);
        return GenericResult.Success();
    }

    /// <summary>Checks that every step a flow names exists and that the order satisfies each one.</summary>
    /// <param name="flow">The flow to check.</param>
    /// <remarks>
    /// Run when configuration loads, so a flow naming a step whose package is absent — or ordering
    /// two steps wrongly — fails at startup with the missing thing named, rather than at 3am on
    /// someone's login. The collection answers by name and what it returns IS the step, so its
    /// declarations are read straight off it.
    /// </remarks>
    private IGenericResult Validate(AuthenticationFlow flow)
    {
        var established = new HashSet<ContextElement>();

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
