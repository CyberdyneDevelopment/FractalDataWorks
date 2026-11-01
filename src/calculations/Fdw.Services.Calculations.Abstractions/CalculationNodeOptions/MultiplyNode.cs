using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// Multiplies two or more numbers left to right in exact decimal arithmetic.
/// </summary>
[TypeOption(typeof(CalculationNodeTypes), "Multiply", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MultiplyNode : FoldNodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MultiplyNode"/> class.
    /// </summary>
    public MultiplyNode()
        : base("Multiply", "Multiplies two or more numbers left to right in exact decimal arithmetic.", typeof(decimal))
    {
    }

    /// <inheritdoc />
    protected override Expression Combine(Expression left, Expression right) => Expression.Multiply(left, right);
}
