namespace Fdw.Data.UI.Components.DataCommand;

/// <summary>Selected field reference for a Query or Insert command.</summary>
public sealed record DataCommandField
{
    /// <summary>Gets or initializes the container alias (from FROM or JOIN).</summary>
    public string Alias { get; init; } = string.Empty;

    /// <summary>Gets or initializes the field name within the aliased container.</summary>
    public string FieldName { get; init; } = string.Empty;

    /// <summary>Gets a display label: <c>alias.fieldName</c>.</summary>
    public string QualifiedName =>
        string.IsNullOrEmpty(Alias) ? FieldName : $"{Alias}.{FieldName}";
}
