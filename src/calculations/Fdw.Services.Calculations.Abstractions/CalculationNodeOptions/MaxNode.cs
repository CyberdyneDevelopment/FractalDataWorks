using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// The larger of two or more numbers.
/// </summary>
[TypeOption(typeof(CalculationNodeTypes), "Max", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MaxNode : FoldNodeBase
{
    private static readonly System.Reflection.MethodInfo Method = typeof(Math).GetMethod(nameof(Math.Max), new[] { typeof(decimal), typeof(decimal) })
        ?? throw new InvalidOperationException("Math.Max(decimal, decimal) was not found");

    /// <summary>
    /// Initializes a new instance of the <see cref="MaxNode"/> class.
    /// </summary>
    public MaxNode()
        : base("Max", "The larger of two or more numbers.", typeof(decimal))
    {
    }

    /// <inheritdoc />
    protected override Expression Combine(Expression left, Expression right) => Expression.Call(Method, left, right);
}
