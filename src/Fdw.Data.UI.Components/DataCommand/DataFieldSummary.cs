using System;

namespace Fdw.Data.UI.Components.DataCommand;

/// <summary>
/// Lightweight field descriptor used by <see cref="DataCommandContext"/> to populate
/// field-picker dropdowns.
/// </summary>
public sealed class DataFieldSummary
{
    /// <summary>Gets or sets the field's unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the field name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the native data type (e.g., "nvarchar", "int", "datetime2"), or <c>null</c> if unknown.</summary>
    public string? DataType { get; set; }

    /// <summary>Gets or sets a value indicating whether the field allows null values.</summary>
    public bool IsNullable { get; set; }

    /// <summary>Gets a display label including the data type: <c>Name (type)</c>.</summary>
    public string DisplayLabel =>
        string.IsNullOrEmpty(DataType)
            ? Name
            : $"{Name} ({DataType})";
}
