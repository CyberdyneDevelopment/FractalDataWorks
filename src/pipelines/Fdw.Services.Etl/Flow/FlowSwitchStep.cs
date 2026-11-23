using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// A switch as a decision (FDW-6134): reads a variable and fires the one output that names its value (without regard to case). A value no outcome names fires <c>otherwise</c> when
/// the switch has one, and fails the run when it does not; a variable with no value fails it too.
/// </summary>
public sealed class FlowSwitchStep : IFlowDecision
{
    private readonly string _variable;
    private readonly IReadOnlyList<string> _outcomes;
    private readonly bool _hasOtherwise;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="FlowSwitchStep"/> class.</summary>
    /// <param name="name">The step's name.</param>
    /// <param name="variable">The variable whose value picks the output.</param>
    /// <param name="outcomes">The outcomes; each is an output.</param>
    /// <param name="hasOtherwise">Whether a value no outcome names fires the output <c>otherwise</c>.</param>
    /// <param name="logger">The logger.</param>
    public FlowSwitchStep(string name, string variable, IReadOnlyList<string> outcomes, bool hasOtherwise, ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(variable);
        Name = name;
        _variable = variable;
        _outcomes = outcomes;
        _hasOtherwise = hasOtherwise;
        _logger = logger ?? NullLogger.Instance;
        Outputs = hasOtherwise ? [.. outcomes, SwitchComponentType.Otherwise] : outcomes;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Outputs { get; }

    /// <inheritdoc />
    public Task<IGenericResult> Run(FlowStepContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Variables.TryGet(_variable, out var read) || read is null)
        {
            return Task.FromResult<IGenericResult>(GenericResult.Failure(FlowLog.SwitchVariableMissing(_logger, Name, _variable)));
        }

        var value = Convert.ToString(read, CultureInfo.InvariantCulture) ?? string.Empty;
        if (Judge(value) is not { } fired)
        {
            return Task.FromResult<IGenericResult>(GenericResult.Failure(FlowLog.SwitchOutcomeUnmatched(_logger, Name, value)));
        }

        foreach (var output in Outputs)
        {
            FlowFired.Set(context.Variables, Name, output, string.Equals(output, fired, StringComparison.Ordinal));
        }

        return Task.FromResult(GenericResult.Success());
    }

    // The switch's own rule: the case that equals the value fires; none equals it, and the step states an otherwise, so that fires; none and no otherwise, nothing fires and the step fails.
    private string? Judge(string value)
    {
        foreach (var outcome in _outcomes)
        {
            if (string.Equals(outcome, value, StringComparison.OrdinalIgnoreCase))
            {
                return outcome;
            }
        }

        return _hasOtherwise ? SwitchComponentType.Otherwise : null;
    }
}
