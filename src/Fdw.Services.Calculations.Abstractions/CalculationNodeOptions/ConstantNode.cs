using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Trees;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// A literal: an exact number, or a true/false condition.
/// </summary>
[TypeOption(typeof(CalculationNodeTypes), "Constant", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ConstantNode : CalculationNodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConstantNode"/> class.
    /// </summary>
    public ConstantNode()
        : base("Constant", "A literal: an exact number, or a true/false condition.", 0, 0)
    {
    }

    /// <inheritdoc />
    public override Type ResultType(CalculationNode node, Type expected, IReadOnlyList<Type> operandTypes) =>
        node.Flag.HasValue ? typeof(bool) : typeof(decimal);

    /// <inheritdoc />
    public override IGenericResult Check(CalculationNode node, ICalculationScope scope) =>
        node.Value.HasValue != node.Flag.HasValue
            ? GenericResult.Success()
            : Invalid(node, "a Constant needs exactly one of a number or a flag");

    /// <inheritdoc />
    public override Expression Emit(NodeEmission emission)
    {
        if (emission.Node.Flag is { } flag)
        {
            return Expression.Constant(flag, typeof(bool));
        }

        return emission.Node.Value is { } value
            ? Expression.Constant(value, typeof(decimal))
            : throw new InvalidOperationException("A Constant without a value reached Emit; Check refuses it");
    }
}
