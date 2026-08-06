namespace Fdw.Data.UI.Components.DataCommand;

/// <summary>Per-field value entry for Insert commands.</summary>
public sealed record DataCommandValueEntry
{
    /// <summary>Gets or initializes the target field name.</summary>
    public string Field { get; init; } = string.Empty;

    /// <summary>Gets or initializes the value (literal or expression).</summary>
    public string Value { get; init; } = string.Empty;
}
