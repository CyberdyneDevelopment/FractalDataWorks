using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Abstractions.Trees;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// Reverses a condition.
/// </summary>
[TypeOption(typeof(CalculationNodeTypes), "Not", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NotNode : CalculationNodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotNode"/> class.
    /// </summary>
    public NotNode()
        : base("Not", "Reverses a condition.", 1, 1)
    {
    }

    /// <inheritdoc />
    public override Type? OperandType(int index) => typeof(bool);

    /// <inheritdoc />
    public override Type ResultType(CalculationNode node, Type expected, IReadOnlyList<Type> operandTypes) => typeof(bool);

    /// <inheritdoc />
    public override Expression Emit(NodeEmission emission) => Expression.Not(emission.Operands[0]);
}
