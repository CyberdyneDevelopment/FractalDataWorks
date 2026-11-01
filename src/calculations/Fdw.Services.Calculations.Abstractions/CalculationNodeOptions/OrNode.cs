using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// True when any condition is true; stops at the first true one.
/// </summary>
[TypeOption(typeof(CalculationNodeTypes), "Or", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class OrNode : FoldNodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrNode"/> class.
    /// </summary>
    public OrNode()
        : base("Or", "True when any condition is true; stops at the first true one.", typeof(bool))
    {
    }

    /// <inheritdoc />
    protected override Expression Combine(Expression left, Expression right) => Expression.OrElse(left, right);
}
