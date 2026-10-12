namespace Fdw.Services.Connections.PostgreSql.Discovery;

/// <summary>
/// Represents a column in a foreign key constraint.
/// </summary>
public sealed class DiscoveredForeignKeyColumn
{
    /// <summary>
    /// Gets the column name in the source (child) table.
    /// </summary>
    public required string ColumnName { get; init; }

    /// <summary>
    /// Gets the referenced column name in the target (parent) table.
    /// </summary>
    public required string ReferencedColumnName { get; init; }

    /// <summary>
    /// Gets the ordinal position of this column in the foreign key (1-based).
    /// </summary>
    public required int Ordinal { get; init; }
}
