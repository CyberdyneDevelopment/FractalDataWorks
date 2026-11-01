using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Abstractions.Trees;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// The first operand that has a value. With a number as the last operand the result always has a value; without one it may
/// be empty.
/// </summary>
[TypeOption(typeof(CalculationNodeTypes), "Coalesce", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CoalesceNode : CalculationNodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CoalesceNode"/> class.
    /// </summary>
    public CoalesceNode()
        : base("Coalesce", "The first operand that has a value.", 2, -1)
    {
    }

    /// <inheritdoc />
    public override Type? OperandType(int index) => null;

    /// <inheritdoc />
    public override Type ResultType(CalculationNode node, Type expected, IReadOnlyList<Type> operandTypes) =>
        operandTypes[operandTypes.Count - 1] == typeof(decimal) ? typeof(decimal) : typeof(decimal?);

    /// <inheritdoc />
    public override Expression Emit(NodeEmission emission)
    {
        var result = emission.Operands[emission.Operands.Count - 1];
        for (var i = emission.Operands.Count - 2; i >= 0; i--)
        {
            result = Expression.Coalesce(Maybe(emission.Operands[i]), result);
        }

        return result;
    }

    private static Expression Maybe(Expression operand) =>
        operand.Type == typeof(decimal?) ? operand : Expression.Convert(operand, typeof(decimal?));
}
