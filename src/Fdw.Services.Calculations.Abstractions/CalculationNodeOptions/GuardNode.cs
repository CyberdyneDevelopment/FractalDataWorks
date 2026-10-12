using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Abstractions.Trees;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// The second operand when the condition is true; otherwise the calculation fails with GuardFailed.
/// </summary>
[TypeOption(typeof(CalculationNodeTypes), "Guard", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GuardNode : CalculationNodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GuardNode"/> class.
    /// </summary>
    public GuardNode()
        : base("Guard", "The second operand when the condition is true; otherwise the calculation fails with GuardFailed.", 2, 2)
    {
    }

    /// <inheritdoc />
    public override Type? OperandType(int index) => index == 0 ? typeof(bool) : null;

    /// <inheritdoc />
    public override Type ResultType(CalculationNode node, Type expected, IReadOnlyList<Type> operandTypes) => operandTypes[1];

    /// <inheritdoc />
    public override Expression Emit(NodeEmission emission) =>
        Expression.Condition(emission.Operands[0], emission.Operands[1], emission.Fail("GuardFailed", emission.Operands[1].Type));
}
