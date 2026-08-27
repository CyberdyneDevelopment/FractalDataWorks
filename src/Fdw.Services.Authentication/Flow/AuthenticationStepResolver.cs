using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Flow;

/// <summary>
/// Finds the step a flow named among those whose packages are referenced.
/// </summary>
/// <remarks>
/// A step is registered by its option at startup, so resolving one is a lookup rather than a
/// configuration read — which is why this is synchronous and why an unknown name is a startup-time
/// problem rather than a runtime one.
/// </remarks>
public sealed class AuthenticationStepResolver : IAuthenticationStepResolver
{
    private readonly ConcurrentDictionary<string, IAuthenticationStep> _steps = new(StringComparer.Ordinal);
    private readonly ILogger<AuthenticationStepResolver> _logger;

    /// <summary>Initializes a new instance of the <see cref="AuthenticationStepResolver"/> class.</summary>
    /// <param name="logger">The logger.</param>
    public AuthenticationStepResolver(ILogger<AuthenticationStepResolver>? logger = null)
        => _logger = logger ?? NullLogger<AuthenticationStepResolver>.Instance;

    /// <summary>Registers <paramref name="step"/> under <paramref name="stepName"/>.</summary>
    /// <param name="stepName">The name a flow will use.</param>
    /// <param name="step">The step.</param>
    /// <remarks>
    /// Two packages claiming one name is refused rather than resolved by order. Whichever won would
    /// depend on assembly load order, and a flow would then mean different things on different hosts.
    /// </remarks>
    public IGenericResult Register(string stepName, IAuthenticationStep step)
    {
        if (string.IsNullOrWhiteSpace(stepName))
            return GenericResult.Failure(StepResolverLog.NameMissing(_logger));

        if (step is null)
            return GenericResult.Failure(StepResolverLog.StepMissing(_logger, stepName));

        if (!_steps.TryAdd(stepName, step))
            return GenericResult.Failure(StepResolverLog.AlreadyRegistered(
                _logger, stepName, _steps[stepName].GetType().Name, step.GetType().Name));

        StepResolverLog.Registered(_logger, stepName, step.GetType().Name);
        return GenericResult.Success();
    }

    /// <inheritdoc />
    public IGenericResult<IAuthenticationStep> Resolve(string stepName)
        => string.IsNullOrWhiteSpace(stepName)
            ? GenericResult<IAuthenticationStep>.Failure(StepResolverLog.NameMissing(_logger))
            : _steps.TryGetValue(stepName, out var step)
                ? GenericResult<IAuthenticationStep>.Success(step)
                : GenericResult<IAuthenticationStep>.Failure(
                    StepResolverLog.NotRegistered(_logger, stepName, Known()));

    /// <summary>Validates that every step a flow names exists and has what it needs.</summary>
    /// <param name="flow">The flow to check.</param>
    /// <remarks>
    /// Run when configuration loads, so a flow naming a step whose package was removed, or ordering
    /// two steps wrongly, fails at startup with the missing thing named — rather than at 3am on
    /// someone's login.
    /// </remarks>
    public IGenericResult Validate(AuthenticationFlow flow)
    {
        ArgumentNullException.ThrowIfNull(flow);

        var established = new HashSet<ContextElement>();

        foreach (var stepName in flow.Steps)
        {
            var resolved = Resolve(stepName);
            if (resolved.IsFailure)
                return GenericResult.Failure(StepResolverLog.NotRegistered(_logger, stepName, Known()));

            var missing = resolved.Value!.Requires.Where(r => !established.Contains(r)).ToList();
            if (missing.Count > 0)
                return GenericResult.Failure(StepResolverLog.OrderInvalid(
                    _logger, flow.Name, stepName, string.Join(", ", missing)));

            foreach (var contributed in resolved.Value!.Contributes)
                established.Add(contributed);
        }

        StepResolverLog.FlowValidated(_logger, flow.Name, flow.Steps.Count);
        return GenericResult.Success();
    }

    private string Known()
        => _steps.IsEmpty ? "(none registered)" : string.Join(", ", _steps.Keys.OrderBy(k => k, StringComparer.Ordinal));
}
