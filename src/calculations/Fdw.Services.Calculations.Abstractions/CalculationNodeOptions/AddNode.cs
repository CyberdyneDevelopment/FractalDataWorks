using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// Adds two or more numbers left to right in exact decimal arithmetic.
/// </summary>
[TypeOption(typeof(CalculationNodeTypes), "Add", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class AddNode : FoldNodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddNode"/> class.
    /// </summary>
    public AddNode()
        : base("Add", "Adds two or more numbers left to right in exact decimal arithmetic.", typeof(decimal))
    {
    }

    /// <inheritdoc />
    protected override Expression Combine(Expression left, Expression right) => Expression.Add(left, right);
}
