using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// Sets a declared variable (FDW-1403): to a value read from the variables in scope, which is how a loop moves on, or to the result
/// of a calculation (FDW-2042). There is one such step; the calculation is optional.
/// </summary>
public sealed class FlowSetVariable : IFlowStep
{
    private readonly string _variable;
    private readonly Func<FlowVariables, object?>? _value;
    private readonly FlowVariableCalculation? _calculation;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="FlowSetVariable"/> class that sets a value.</summary>
    /// <param name="name">The step's name.</param>
    /// <param name="variable">The variable to set; it must be declared in scope when the step runs.</param>
    /// <param name="value">Computes the new value from the variables in scope.</param>
    /// <param name="logger">The logger.</param>
    public FlowSetVariable(string name, string variable, Func<FlowVariables, object?> value, ILogger? logger = null)
        : this(name, variable, value ?? throw new ArgumentNullException(nameof(value)), null, logger)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FlowSetVariable"/> class that sets the result of a calculation.</summary>
    /// <param name="name">The step's name.</param>
    /// <param name="variable">The variable to set; it must be declared in scope when the step runs.</param>
    /// <param name="calculation">Works the new value out from the variables in scope.</param>
    /// <param name="logger">The logger.</param>
    public FlowSetVariable(string name, string variable, FlowVariableCalculation calculation, ILogger? logger = null)
        : this(name, variable, null, calculation ?? throw new ArgumentNullException(nameof(calculation)), logger)
    {
    }

    private FlowSetVariable(string name, string variable, Func<FlowVariables, object?>? value, FlowVariableCalculation? calculation, ILogger? logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(variable);
        Name = name;
        _variable = variable;
        _value = value;
        _calculation = calculation;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public async Task<IGenericResult> Run(FlowStepContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var target = FlowVariableTarget.Check(context.Variables, Name, _variable, _logger);
        if (target.IsFailure)
        {
            return target;
        }

        if (_calculation is null)
        {
            context.Variables.TrySet(_variable, _value!(context.Variables));
            return GenericResult.Success();
        }

        var worked = await _calculation.Work(Name, context.Variables, cancellationToken).ConfigureAwait(false);
        if (worked.IsFailure)
        {
            return worked;
        }

        context.Variables.TrySet(_variable, worked.Value);
        return GenericResult.Success();
    }
}
