namespace Fdw.Data.UI.Components.DataCommand;

/// <summary>Single filter clause: field operator value.</summary>
public sealed record DataCommandFilterClause
{
    /// <summary>Gets or initializes the field reference as <c>alias.fieldName</c> or plain <c>fieldName</c>.</summary>
    public string Field { get; init; } = string.Empty;

    /// <summary>Gets or initializes the operator name from FilterOperators TypeCollection (e.g., "Equal").</summary>
    public string Operator { get; init; } = string.Empty;

    /// <summary>Gets or initializes the filter value as a string literal.</summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes a value indicating whether this clause combines with the previous clause
    /// using OR (<c>true</c>) or AND (<c>false</c>, the default).
    /// </summary>
    public bool UseOr { get; init; }
}
