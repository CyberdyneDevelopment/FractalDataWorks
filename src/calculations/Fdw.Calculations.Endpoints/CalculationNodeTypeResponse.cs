using System.Collections.Generic;

namespace Fdw.Calculations.Endpoints;

/// <summary>One operation a formula node can name, as offered to the people building a formula (FDW-1280).</summary>
public class CalculationNodeTypeResponse
{
    /// <summary>Gets or sets the operation's name: the <c>op</c> a node states, such as <c>Add</c> or <c>Round</c>.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets what the operation does, in one sentence.</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Gets or sets the fewest operands a node of this operation takes.</summary>
    public int MinOperands { get; set; }

    /// <summary>Gets or sets the most operands a node of this operation takes.</summary>
    public int MaxOperands { get; set; }

    /// <summary>
    /// Gets or sets the names the operands have in JSON (for example <c>condition</c>, <c>then</c>), or an empty list when they are
    /// an unnamed <c>args</c> array.
    /// </summary>
    public List<string> OperandNames { get; set; } = new();

    /// <summary>
    /// Gets or sets the optional members of a node that this operation reads, by their JSON names: <c>name</c>, <c>value</c>,
    /// <c>flag</c>, <c>places</c> and <c>mode</c>. Empty when the operation takes only operands.
    /// </summary>
    public List<string> Members { get; set; } = new();

    /// <summary>Gets or sets the values the <c>mode</c> member may take; empty when the operation reads no <c>mode</c>.</summary>
    public List<string> Modes { get; set; } = new();
}
