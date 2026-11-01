using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Abstractions.Trees;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// The second operand when the condition is true, otherwise the third. Only the chosen branch is evaluated, so a branch
/// that would fail is not run when it is not chosen.
/// </summary>
[TypeOption(typeof(CalculationNodeTypes), "When", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WhenNode : CalculationNodeBase
{
    private static readonly string[] ClauseNames = new[] { "if", "then", "else" };

    /// <summary>
    /// Initializes a new instance of the <see cref="WhenNode"/> class.
    /// </summary>
    public WhenNode()
        : base("When", "The second operand when the condition is true, otherwise the third.", 3, 3)
    {
    }

    /// <inheritdoc />
    public override IReadOnlyList<string> OperandNames => ClauseNames;

    /// <inheritdoc />
    public override Type? OperandType(int index) => index == 0 ? typeof(bool) : null;

    /// <inheritdoc />
    public override Type ResultType(CalculationNode node, Type expected, IReadOnlyList<Type> operandTypes) =>
        operandTypes[1] == typeof(decimal) && operandTypes[2] == typeof(decimal) ? typeof(decimal) : typeof(decimal?);

    /// <inheritdoc />
    public override Expression Emit(NodeEmission emission)
    {
        var type = emission.Operands[1].Type == typeof(decimal) && emission.Operands[2].Type == typeof(decimal)
            ? typeof(decimal)
            : typeof(decimal?);
        return Expression.Condition(
            emission.Operands[0],
            CalculationCoercion.Coerce(emission.Operands[1], type, emission.State, 0),
            CalculationCoercion.Coerce(emission.Operands[2], type, emission.State, 0));
    }
}
