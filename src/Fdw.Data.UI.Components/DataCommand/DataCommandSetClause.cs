namespace Fdw.Data.UI.Components.DataCommand;

/// <summary>SET clause for Update and Upsert commands.</summary>
public sealed record DataCommandSetClause
{
    /// <summary>Gets or initializes the target field name.</summary>
    public string Field { get; init; } = string.Empty;

    /// <summary>Gets or initializes the value expression (literal or source field reference).</summary>
    public string ValueOrExpr { get; init; } = string.Empty;
}
