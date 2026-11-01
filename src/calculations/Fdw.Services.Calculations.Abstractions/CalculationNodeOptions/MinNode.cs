using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// The smaller of two or more numbers.
/// </summary>
[TypeOption(typeof(CalculationNodeTypes), "Min", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MinNode : FoldNodeBase
{
    private static readonly System.Reflection.MethodInfo Method = typeof(Math).GetMethod(nameof(Math.Min), new[] { typeof(decimal), typeof(decimal) })
        ?? throw new InvalidOperationException("Math.Min(decimal, decimal) was not found");

    /// <summary>
    /// Initializes a new instance of the <see cref="MinNode"/> class.
    /// </summary>
    public MinNode()
        : base("Min", "The smaller of two or more numbers.", typeof(decimal))
    {
    }

    /// <inheritdoc />
    protected override Expression Combine(Expression left, Expression right) => Expression.Call(Method, left, right);
}
