namespace Fdw.Schema.Clients.Models;

/// <summary>
/// A single filter condition for data preview queries.
/// </summary>
public sealed class PreviewFilterCondition
{
    /// <summary>Gets or sets the column name to filter on.</summary>
    public string Column { get; set; } = string.Empty;
    /// <summary>Gets or sets the filter operator (e.g. Equal, Contains, GreaterThan).</summary>
    public string Operator { get; set; } = "Equal";
    /// <summary>Gets or sets the filter value.</summary>
    public object? Value { get; set; }
}
