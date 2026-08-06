namespace Fdw.Data.UI.Components.DataCommand;

/// <summary>Sort clause for a Query command.</summary>
public sealed record DataCommandSort
{
    /// <summary>Gets or initializes the field reference as <c>alias.fieldName</c> or plain name.</summary>
    public string Field { get; init; } = string.Empty;

    /// <summary>Gets or initializes the sort direction: "Asc" or "Desc".</summary>
    public string Direction { get; init; } = "Asc";
}
