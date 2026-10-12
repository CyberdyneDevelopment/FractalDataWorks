using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Fdw.Services.Calculations.Abstractions.Trees;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// An operator that compares two numbers and yields a condition. A comparison with an empty operand is false.
/// </summary>
/// <remarks>
/// Why empty compares false and not as zero: an empty value is not a number, so no ordering or equality holds. To test for
/// empty, the author writes IsEmpty. The operation each comparison runs has the operator's own name in
/// <c>CalculationRuntime</c>.
/// </remarks>
public abstract class ComparisonNodeBase : CalculationNodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ComparisonNodeBase"/> class.
    /// </summary>
    /// <param name="name">The operator's name, which is also the name of its runtime operation.</param>
    /// <param name="summary">A one-sentence description.</param>
    protected ComparisonNodeBase(string name, string summary)
        : base(name, summary, 2, 2)
    {
    }

    /// <inheritdoc />
    public override Type? OperandType(int index) => typeof(decimal?);

    /// <inheritdoc />
    public override Type ResultType(CalculationNode node, Type expected, IReadOnlyList<Type> operandTypes) => typeof(bool);

    /// <inheritdoc />
    public override Expression Emit(NodeEmission emission) =>
        NodeEmission.Call(Name, emission.Operands[0], emission.Operands[1]);
}
