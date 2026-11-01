using System;
using System.Collections.Generic;

namespace Fdw.Services.Calculations.Abstractions.Trees;

/// <summary>
/// One node of a calculation tree: an operator, the members that operator reads, and its operands.
/// </summary>
/// <remarks>
/// Why a tree and not a flat list of steps: parentheses, conditions and nesting are the structure of the tree, so nothing is
/// parsed and nothing depends on an operator precedence. The tree is immutable and is stored and exchanged as JSON
/// (<see cref="CalculationTreeJson"/>). Which members an operator reads, and how many operands it takes, is stated by its
/// <c>CalculationNodeTypes</c> option, not here.
/// </remarks>
public sealed class CalculationNode
{
    private static readonly CalculationNode[] NoOperands = Array.Empty<CalculationNode>();

    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationNode"/> class.
    /// </summary>
    /// <param name="op">The operator, the name of a <c>CalculationNodeTypes</c> option.</param>
    /// <param name="name">The field or result name for Field and Ref; otherwise null.</param>
    /// <param name="value">The numeric literal of a Constant; otherwise null.</param>
    /// <param name="flag">The boolean literal of a Constant; otherwise null.</param>
    /// <param name="places">The decimal places of a Round; otherwise null.</param>
    /// <param name="mode">The midpoint rounding mode of a Round; otherwise null.</param>
    /// <param name="operands">The operands, in order.</param>
    public CalculationNode(
        string op,
        string? name,
        decimal? value,
        bool? flag,
        int? places,
        string? mode,
        IReadOnlyList<CalculationNode> operands)
    {
        Op = op;
        Name = name;
        Value = value;
        Flag = flag;
        Places = places;
        Mode = mode;
        Operands = operands;
    }

    /// <summary>Gets the operator, the name of a <c>CalculationNodeTypes</c> option.</summary>
    public string Op { get; }

    /// <summary>Gets the field or result name of a Field or Ref node.</summary>
    public string? Name { get; }

    /// <summary>Gets the numeric literal of a Constant node.</summary>
    public decimal? Value { get; }

    /// <summary>Gets the boolean literal of a Constant node.</summary>
    public bool? Flag { get; }

    /// <summary>Gets the decimal places of a Round node.</summary>
    public int? Places { get; }

    /// <summary>Gets the midpoint rounding mode of a Round node.</summary>
    public string? Mode { get; }

    /// <summary>Gets the operands, in order.</summary>
    public IReadOnlyList<CalculationNode> Operands { get; }

    /// <summary>Creates a node with an operator and operands only.</summary>
    /// <param name="op">The operator.</param>
    /// <param name="operands">The operands, in order.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Of(string op, params CalculationNode[] operands) =>
        new(op, null, null, null, null, null, operands);

    /// <summary>Creates a node that names a field or a result and has no operands.</summary>
    /// <param name="op">The operator, Field or Ref.</param>
    /// <param name="name">The field or result name.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Named(string op, string name) =>
        new(op, name, null, null, null, null, NoOperands);

    /// <summary>Creates a node that works with a moment in time: a time zone in <c>name</c> and an anchor or unit in <c>mode</c>.</summary>
    /// <param name="op">The operator, Now or AddTime.</param>
    /// <param name="timeZone">The IANA id of the time zone.</param>
    /// <param name="mode">The anchor of a Now node or the unit of an AddTime node; null for a Now node cut to no anchor.</param>
    /// <param name="operands">The operands, in order.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Zoned(string op, string timeZone, string? mode, params CalculationNode[] operands) =>
        new(op, timeZone, null, null, null, mode, operands);

    /// <summary>Creates a numeric Constant.</summary>
    /// <param name="value">The literal.</param>
    /// <returns>The node.</returns>
    public static CalculationNode FromDecimal(decimal value) =>
        new("Constant", null, value, null, null, null, NoOperands);

    /// <summary>Creates a numeric Constant from a whole number.</summary>
    /// <param name="value">The literal.</param>
    /// <returns>The node.</returns>
    public static CalculationNode FromInt32(int value) => FromDecimal(value);

    /// <summary>Creates a boolean Constant.</summary>
    /// <param name="flag">The literal.</param>
    /// <returns>The node.</returns>
    public static CalculationNode FromBoolean(bool flag) =>
        new("Constant", null, null, flag, null, null, NoOperands);

    /// <summary>Creates a Round node.</summary>
    /// <param name="operand">The value to round.</param>
    /// <param name="places">The decimal places.</param>
    /// <param name="mode">The midpoint rounding mode, a <see cref="System.MidpointRounding"/> name.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Rounded(CalculationNode operand, int places, string mode) =>
        new("Round", null, null, null, places, mode, new[] { operand });

    /// <summary>Converts a decimal literal to a Constant node.</summary>
    /// <param name="value">The literal.</param>
    public static implicit operator CalculationNode(decimal value) => FromDecimal(value);

    /// <summary>Converts a whole-number literal to a Constant node.</summary>
    /// <param name="value">The literal.</param>
    public static implicit operator CalculationNode(int value) => FromInt32(value);

    /// <summary>Gets the node as a person reads it in a refusal: the operator, and the name when it has one.</summary>
    /// <returns>The description.</returns>
    public string Describe() => Name is null ? Op : string.Concat(Op, "(", Name, ")");
}
