namespace Fdw.Services.Data.Clients.Models;

/// <summary>A total or a lookup that the formulas of a Built transformation can read by name.</summary>
public sealed class DerivedValuePayload
{
    /// <summary>Gets or sets the name formulas read the value under; it must differ from every formula column's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind: Sum, Count, Min, Max or Average over a column, or Lookup of a value in another table.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the table the value is taken from.</summary>
    public string Table { get; set; } = string.Empty;

    /// <summary>Gets or sets the column that is totalled, or, for a lookup, the column whose value is taken.</summary>
    public string Column { get; set; } = string.Empty;

    /// <summary>Gets or sets a lookup's key column in the searched table; null for a total.</summary>
    public string? KeyColumn { get; set; }

    /// <summary>Gets or sets the name of the current row's value a lookup searches for; null for a total.</summary>
    public string? MatchField { get; set; }
}
