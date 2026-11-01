using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Abstractions.Trees;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// True when the operand has no value.
/// </summary>
[TypeOption(typeof(CalculationNodeTypes), "IsEmpty", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class IsEmptyNode : CalculationNodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IsEmptyNode"/> class.
    /// </summary>
    public IsEmptyNode()
        : base("IsEmpty", "True when the operand has no value.", 1, 1)
    {
    }

    /// <inheritdoc />
    public override Type? OperandType(int index) => typeof(decimal?);

    /// <inheritdoc />
    public override Type ResultType(CalculationNode node, Type expected, IReadOnlyList<Type> operandTypes) => typeof(bool);

    /// <inheritdoc />
    public override Expression Emit(NodeEmission emission) =>
        Expression.Not(Expression.Property(emission.Operands[0], nameof(Nullable<decimal>.HasValue)));
}
