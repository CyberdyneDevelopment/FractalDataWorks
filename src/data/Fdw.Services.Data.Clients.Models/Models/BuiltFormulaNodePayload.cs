using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// One piece of a Built transformation's formula: an operator with its operands, or a leaf that reads a field or states a
/// constant. A formula is a tree of these.
/// </summary>
public sealed class BuiltFormulaNodePayload
{
    /// <summary>Gets or sets the operator: the name of a calculation node type, such as Field, Constant, Add, Multiply, Round or When.</summary>
    public string Op { get; set; } = string.Empty;

    /// <summary>Gets or sets the name a Field reads (a declared input) or a Ref refers to (an earlier result); otherwise null.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the number a Constant states; otherwise null.</summary>
    public decimal? Value { get; set; }

    /// <summary>Gets or sets the true or false a Constant states; otherwise null.</summary>
    public bool? Flag { get; set; }

    /// <summary>Gets or sets the decimal places a Round keeps; otherwise null.</summary>
    public int? Places { get; set; }

    /// <summary>Gets or sets the midpoint rounding mode of a Round; otherwise null.</summary>
    public string? Mode { get; set; }

    /// <summary>Gets or sets the operands, in order; empty for a leaf.</summary>
    public IList<BuiltFormulaNodePayload> Operands { get; set; } = [];
}
