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
    // Types rather than instances, because a step reads through scoped providers while this map
    // is the same for every request. The instance comes from the scope that asks for one.
    private static readonly ConcurrentDictionary<string, Type> Registered = new(StringComparer.Ordinal);

    // Instances registered directly, for a step that is already built - a test double, or a step
    // with no scoped dependency to resolve. Checked before the type map so a caller that supplied
    // an instance gets that instance rather than a second one from the container.
    private readonly ConcurrentDictionary<string, IAuthenticationStep> _instances = new(StringComparer.Ordinal);

    private readonly IServiceProvider? _services;
    private readonly ILogger<AuthenticationStepResolver> _logger;

    /// <summary>Initializes a new instance of the <see cref="AuthenticationStepResolver"/> class.</summary>
    /// <param name="services">The scope steps are resolved from.</param>
    /// <param name="logger">The logger.</param>
    public AuthenticationStepResolver(
        IServiceProvider? services = null,
        ILogger<AuthenticationStepResolver>? logger = null)
    {
        _services = services;
        _logger = logger ?? NullLogger<AuthenticationStepResolver>.Instance;
    }

    /// <summary>Registers an already-built <paramref name="step"/> under <paramref name="stepName"/>.</summary>
    /// <param name="stepName">The name a flow will use.</param>
    /// <param name="step">The step.</param>
    /// <remarks>
    /// For a step that needs no scope to build — one configured in place, or a double in a test.
    /// A step reading through scoped providers registers its type instead, so the instance comes
    /// from the scope that asks for it rather than outliving one.
    /// </remarks>
    public IGenericResult Register(string stepName, IAuthenticationStep step)
    {
        if (string.IsNullOrWhiteSpace(stepName))
            return GenericResult.Failure(StepResolverLog.NameMissing(_logger));

        if (step is null)
            return GenericResult.Failure(StepResolverLog.StepMissing(_logger, stepName));

        if (!_instances.TryAdd(stepName, step))
            return GenericResult.Failure(StepResolverLog.AlreadyRegistered(
                _logger, stepName, _instances[stepName].GetType().Name, step.GetType().Name));

        StepResolverLog.Registered(_logger, stepName, step.GetType().Name);
        return GenericResult.Success();
    }

    /// <summary>Registers the type serving <paramref name="stepName"/>.</summary>
    /// <param name="stepName">The name a flow will use.</param>
    /// <param name="stepType">The type implementing it.</param>
    /// <remarks>
    /// Two packages claiming one name is refused rather than resolved by order. Whichever won would
    /// depend on assembly load order, and a flow would then mean different things on different hosts.
    /// Registering the same type under the same name twice is not a conflict — a host that
    /// initializes more than once is repeating itself, not disagreeing with itself.
    /// </remarks>
    public IGenericResult Register(string stepName, Type stepType)
    {
        if (string.IsNullOrWhiteSpace(stepName))
            return GenericResult.Failure(StepResolverLog.NameMissing(_logger));

        if (stepType is null)
            return GenericResult.Failure(StepResolverLog.StepMissing(_logger, stepName));

        if (!Registered.TryAdd(stepName, stepType) && Registered[stepName] != stepType)
            return GenericResult.Failure(StepResolverLog.AlreadyRegistered(
                _logger, stepName, Registered[stepName].Name, stepType.Name));

        StepResolverLog.Registered(_logger, stepName, stepType.Name);
        return GenericResult.Success();
    }

    /// <inheritdoc />
    public IGenericResult<IAuthenticationStep> Resolve(string stepName)
    {
        if (string.IsNullOrWhiteSpace(stepName))
            return GenericResult<IAuthenticationStep>.Failure(StepResolverLog.NameMissing(_logger));

        if (_instances.TryGetValue(stepName, out var instance))
            return GenericResult<IAuthenticationStep>.Success(instance);

        if (!Registered.TryGetValue(stepName, out var stepType))
            return GenericResult<IAuthenticationStep>.Failure(
                StepResolverLog.NotRegistered(_logger, stepName, Known()));

        return _services?.GetService(stepType) is IAuthenticationStep step
            ? GenericResult<IAuthenticationStep>.Success(step)
            : GenericResult<IAuthenticationStep>.Failure(
                StepResolverLog.StepMissing(_logger, stepName));
    }

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
    {
        var names = Registered.Keys.Concat(_instances.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();

        return names.Count == 0 ? "(none registered)" : string.Join(", ", names);
    }
}
