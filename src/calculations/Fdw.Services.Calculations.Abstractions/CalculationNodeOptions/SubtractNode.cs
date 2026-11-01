using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// Subtracts the second and every later number from the first, left to right, in exact decimal arithmetic.
/// </summary>
[TypeOption(typeof(CalculationNodeTypes), "Subtract", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SubtractNode : FoldNodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SubtractNode"/> class.
    /// </summary>
    public SubtractNode()
        : base("Subtract", "Subtracts the second and every later number from the first, left to right, in exact decimal arithmetic.", typeof(decimal))
    {
    }

    /// <inheritdoc />
    protected override Expression Combine(Expression left, Expression right) => Expression.Subtract(left, right);
}
