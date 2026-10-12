using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Fdw.Services.Calculations.Abstractions.Trees;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// An operator that combines two or more operands of one type left to right: Add, Subtract, Multiply, Max, Min, And, Or.
/// </summary>
/// <remarks>
/// Why n-ary: a sum of forty terms is one node with forty operands, not a chain of thirty-nine binary nodes, so the tree
/// stays flat and the author's order of evaluation is the order written.
/// </remarks>
public abstract class FoldNodeBase : CalculationNodeBase
{
    private readonly Type _operandType;

    /// <summary>
    /// Initializes a new instance of the <see cref="FoldNodeBase"/> class.
    /// </summary>
    /// <param name="name">The operator's name.</param>
    /// <param name="summary">A one-sentence description.</param>
    /// <param name="operandType">The type every operand has, and the operator yields: decimal or bool.</param>
    protected FoldNodeBase(string name, string summary, Type operandType)
        : base(name, summary, 2, -1)
    {
        _operandType = operandType;
    }

    /// <inheritdoc />
    public override Type? OperandType(int index) => _operandType;

    /// <inheritdoc />
    public override Type ResultType(CalculationNode node, Type expected, IReadOnlyList<Type> operandTypes) => _operandType;

    /// <inheritdoc />
    public override Expression Emit(NodeEmission emission)
    {
        var result = emission.Operands[0];
        for (var i = 1; i < emission.Operands.Count; i++)
        {
            result = Combine(result, emission.Operands[i]);
        }

        return result;
    }

    /// <summary>Combines the value so far with the next operand.</summary>
    /// <param name="left">The value so far.</param>
    /// <param name="right">The next operand.</param>
    /// <returns>The combined expression.</returns>
    protected abstract Expression Combine(Expression left, Expression right);
}
