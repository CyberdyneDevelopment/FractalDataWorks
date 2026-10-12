namespace Fdw.Services.Data.Clients.Models;

/// <summary>One result of a Built transformation and the formula that produces it.</summary>
public sealed class BuiltResultPayload
{
    /// <summary>Gets or sets the result name; a later result's formula may refer to it with a Ref, and a kept result becomes a column of that name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets a value saying whether the result is written out; false for a working number only later results use.</summary>
    public bool IsKept { get; set; }

    /// <summary>Gets or sets the formula, as a tree.</summary>
    public BuiltFormulaNodePayload? Formula { get; set; }
}
