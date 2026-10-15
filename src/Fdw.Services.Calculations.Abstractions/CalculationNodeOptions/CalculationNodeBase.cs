using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Trees;
using Fdw.Services.Calculations.Abstractions.TreeResultCodes;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// Base class of the operators a calculation tree can use (CRTP). An operator states its shape and emits itself; the
/// compiler never switches on an operator's name.
/// </summary>
public abstract class CalculationNodeBase : TypeOptionBase<CalculationNodeBase>, ICalculationNodeType
{
    private static readonly string[] NoNames = Array.Empty<string>();

    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationNodeBase"/> class.
    /// </summary>
    /// <param name="name">The operator's name, as a tree states it.</param>
    /// <param name="summary">A one-sentence description of the operator.</param>
    /// <param name="minOperands">The fewest operands it takes.</param>
    /// <param name="maxOperands">The most operands it takes, or -1 for no upper limit.</param>
    protected CalculationNodeBase(string name, string summary, int minOperands, int maxOperands)
        : base(name)
    {
        Summary = summary;
        MinOperands = minOperands;
        MaxOperands = maxOperands;
    }

    /// <inheritdoc />
    public string Summary { get; }

    /// <inheritdoc />
    public int MinOperands { get; }

    /// <inheritdoc />
    public int MaxOperands { get; }

    /// <inheritdoc />
    public virtual IReadOnlyList<string> OperandNames => NoNames;

    /// <inheritdoc />
    public virtual IReadOnlyList<string> Members => NoNames;

    /// <inheritdoc />
    public virtual IReadOnlyList<string> Modes => NoNames;

    /// <inheritdoc />
    public virtual Type? OperandType(int index) => typeof(decimal);

    /// <inheritdoc />
    public virtual Type ResultType(CalculationNode node, Type expected, IReadOnlyList<Type> operandTypes) => typeof(decimal);

    /// <inheritdoc />
    public virtual IGenericResult Check(CalculationNode node, ICalculationScope scope) => GenericResult.Success();

    /// <inheritdoc />
    public abstract Expression Emit(NodeEmission emission);

    /// <summary>Builds the refusal for a node member that is missing, unwanted or out of range.</summary>
    /// <param name="node">The node.</param>
    /// <param name="reason">What is wrong.</param>
    /// <returns>The failed result.</returns>
    protected static IGenericResult Invalid(CalculationNode node, string reason) =>
        GenericResult.Failure(
            CalculationTreeResultCodes.ByName("NodeMemberInvalid"),
            ResultDetails.Create("node", node.Describe(), "reason", reason));
}
