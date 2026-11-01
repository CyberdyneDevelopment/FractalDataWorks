using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Trees;

namespace Fdw.Services.Calculations.Abstractions.Fluency;

/// <summary>
/// Collects the named results of a calculation array in the order they are evaluated and checks them on <see cref="Build(CalculationLimits)"/>.
/// </summary>
public sealed class CalculationArrayBuilder
{
    private readonly string _name;
    private readonly List<CalculationResultDefinition> _results = new();

    internal CalculationArrayBuilder(string name)
    {
        _name = name;
    }

    /// <summary>Adds a result. It is evaluated after the results added before it, which it may use with a Ref node.</summary>
    /// <param name="name">The result name.</param>
    /// <param name="expression">The tree that computes it.</param>
    /// <param name="emit">True when the run writes it out; false for an intermediate that only later results use.</param>
    /// <returns>This builder.</returns>
    public CalculationArrayBuilder Result(string name, CalculationNode expression, bool emit = true)
    {
        _results.Add(new CalculationResultDefinition(name, expression, emit));
        return this;
    }

    /// <summary>Checks the array against the stated limits and returns it.</summary>
    /// <param name="limits">The stated limits.</param>
    /// <returns>
    /// The array, or a coded refusal: an unknown operator, a wrong arity or operand type, a reference to a result not defined
    /// earlier, a duplicate or shadowing name, or a limit exceeded.
    /// </returns>
    public IGenericResult<CalculationArrayDefinition> Build(CalculationLimits limits) => Build(limits, allowDateTimeResults: false);

    /// <summary>Checks the array against the stated limits, saying whether a result may be a date-time, and returns it.</summary>
    /// <param name="limits">The stated limits.</param>
    /// <param name="allowDateTimeResults">True when the caller reads a date-time result back through its value kind.</param>
    /// <returns>The array, or a coded refusal.</returns>
    public IGenericResult<CalculationArrayDefinition> Build(CalculationLimits limits, bool allowDateTimeResults)
    {
        var array = new CalculationArrayDefinition(_name, _results.ToArray());
        var validation = CalculationArrayValidator.Validate(array, limits, allowDateTimeResults);
        return validation.IsFailure
            ? validation.ToNewResult<CalculationArrayDefinition>()
            : GenericResult<CalculationArrayDefinition>.Success(array);
    }
}
