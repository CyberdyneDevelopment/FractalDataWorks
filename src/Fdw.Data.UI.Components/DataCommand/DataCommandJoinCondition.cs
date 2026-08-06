namespace Fdw.Data.UI.Components.DataCommand;

/// <summary>Single equi-join condition: left.field = right.field.</summary>
public sealed record DataCommandJoinCondition
{
    /// <summary>Gets or sets the left-side field reference as <c>alias.fieldName</c>.</summary>
    public string LeftField { get; init; } = string.Empty;

    /// <summary>Gets or sets the operator name (typically "Equal").</summary>
    public string Operator { get; init; } = "Equal";

    /// <summary>Gets or sets the right-side field reference as <c>alias.fieldName</c>.</summary>
    public string RightField { get; init; } = string.Empty;
}
