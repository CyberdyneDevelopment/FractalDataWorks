using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Abstractions.Runtime;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// Divides the first number by the second in exact decimal arithmetic. A zero divisor fails with DivisionByZero; to avoid
/// the failure, state a condition with When.
/// </summary>
[TypeOption(typeof(CalculationNodeTypes), "Divide", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DivideNode : CalculationNodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DivideNode"/> class.
    /// </summary>
    public DivideNode()
        : base("Divide", "Divides the first number by the second; a zero divisor fails with DivisionByZero.", 2, 2)
    {
    }

    /// <inheritdoc />
    public override Expression Emit(NodeEmission emission) =>
        NodeEmission.Call(nameof(CalculationRuntime.Divide), emission.Operands[0], emission.Operands[1], emission.State, emission.Index);
}
