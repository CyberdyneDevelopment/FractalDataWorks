using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Trees;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// An operator a calculation tree can use: how many operands it takes, what types they must have, what type it yields,
/// how to check a node of it, and how to emit it as an expression.
/// </summary>
public interface ICalculationNodeType : ITypeOption<int, CalculationNodeBase>
{
    /// <summary>Gets a one-sentence description of what the operator does.</summary>
    string Summary { get; }

    /// <summary>Gets the fewest operands the operator takes.</summary>
    int MinOperands { get; }

    /// <summary>Gets the most operands the operator takes, or -1 when there is no upper limit.</summary>
    int MaxOperands { get; }

    /// <summary>Gets the names the operands have in JSON, or an empty list when they are an unnamed "args" array.</summary>
    IReadOnlyList<string> OperandNames { get; }

    /// <summary>
    /// Gets the optional members of a node that this operation reads, by their JSON names: <c>name</c>, <c>value</c>, <c>flag</c>,
    /// <c>places</c> and <c>mode</c>. Empty when the operation takes only operands. A client builds its forms from this list and
    /// never from the operation's name (FDW-1280).
    /// </summary>
    IReadOnlyList<string> Members { get; }

    /// <summary>Gets the values the <c>mode</c> member may take, or an empty list when the operation reads no <c>mode</c>.</summary>
    IReadOnlyList<string> Modes { get; }

    /// <summary>Gets the type operand <paramref name="index"/> must have, or null when any numeric operand is accepted as it is.</summary>
    /// <param name="index">The operand's position.</param>
    /// <returns>decimal, decimal? or bool; or null.</returns>
    Type? OperandType(int index);

    /// <summary>Gets the type a node of this operator yields.</summary>
    /// <param name="node">The node.</param>
    /// <param name="expected">The type the parent asks for.</param>
    /// <param name="operandTypes">The types the operands yield, before conversion to the required types.</param>
    /// <returns>decimal, decimal? or bool.</returns>
    Type ResultType(CalculationNode node, Type expected, IReadOnlyList<Type> operandTypes);

    /// <summary>Checks the members of a node (name, value, places, mode) after its arity was checked.</summary>
    /// <param name="node">The node.</param>
    /// <param name="scope">The array the node sits in.</param>
    /// <returns>Success, or the refusal.</returns>
    IGenericResult Check(CalculationNode node, ICalculationScope scope);

    /// <summary>Emits the node as an expression, given its operands already emitted and converted.</summary>
    /// <param name="emission">The node, its operands and the compile context.</param>
    /// <returns>The expression.</returns>
    Expression Emit(NodeEmission emission);
}
