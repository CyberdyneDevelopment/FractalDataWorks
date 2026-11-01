namespace Fdw.Services.Data.Clients.Models;

/// <summary>One column of a table output: copied from a visible value, a stated value, or the result of a formula.</summary>
public sealed class OutputColumnPayload
{
    /// <summary>Gets or sets the column's name in the output.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind: Copy, Literal or Formula.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the visible value a Copy column carries through (Copy only).</summary>
    public string? Source { get; set; }

    /// <summary>Gets or sets the type of a Literal column's value: Text, Number or Flag (Literal only).</summary>
    public string? LiteralType { get; set; }

    /// <summary>Gets or sets the value of a Literal column, written as text (Literal only).</summary>
    public string? LiteralValue { get; set; }

    /// <summary>Gets or sets the formula of a Formula column, as a tree (Formula only).</summary>
    public BuiltFormulaNodePayload? Formula { get; set; }
}
