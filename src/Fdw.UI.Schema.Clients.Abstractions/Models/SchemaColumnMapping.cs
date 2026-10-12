namespace Fdw.UI.Schema.Clients.Models;

/// <summary>
/// Maps a foreign key column to the corresponding primary key column in a relationship.
/// </summary>
public sealed class SchemaColumnMapping
{
    /// <summary>
    /// Gets or sets the column name in the foreign (referencing) table.
    /// </summary>
    public string ForeignColumn { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the column name in the primary (referenced) table.
    /// </summary>
    public string PrimaryColumn { get; set; } = string.Empty;
}
