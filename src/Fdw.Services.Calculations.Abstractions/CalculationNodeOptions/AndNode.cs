using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// True when every condition is true; stops at the first false one.
/// </summary>
[TypeOption(typeof(CalculationNodeTypes), "And", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class AndNode : FoldNodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AndNode"/> class.
    /// </summary>
    public AndNode()
        : base("And", "True when every condition is true; stops at the first false one.", typeof(bool))
    {
    }

    /// <inheritdoc />
    protected override Expression Combine(Expression left, Expression right) => Expression.AndAlso(left, right);
}
