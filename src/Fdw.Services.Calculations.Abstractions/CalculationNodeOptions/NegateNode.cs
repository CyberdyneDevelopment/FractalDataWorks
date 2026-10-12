using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// Reverses the sign of a number.
/// </summary>
[TypeOption(typeof(CalculationNodeTypes), "Negate", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NegateNode : CalculationNodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NegateNode"/> class.
    /// </summary>
    public NegateNode()
        : base("Negate", "Reverses the sign of a number.", 1, 1)
    {
    }

    /// <inheritdoc />
    public override Expression Emit(NodeEmission emission) => Expression.Negate(emission.Operands[0]);
}
