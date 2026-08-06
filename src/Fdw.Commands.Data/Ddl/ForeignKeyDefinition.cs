namespace Fdw.Commands.Data.Ddl;

/// <summary>
/// Metadata for a foreign key constraint definition.
/// </summary>
/// <remarks>
/// <para>
/// Describes a foreign key relationship between tables.
/// Translators convert this to backend-specific constraint syntax.
/// </para>
/// </remarks>
public sealed class ForeignKeyDefinition
{
    /// <summary>
    /// Gets or sets the name of the foreign key constraint.
    /// </summary>
    /// <value>The constraint name, or null to auto-generate.</value>
    public string? Name { get; init; }

    /// <summary>
    /// Gets or sets the column name in the current table.
    /// </summary>
    /// <value>The name of the foreign key column.</value>
    public required string ColumnName { get; init; }

    /// <summary>
    /// Gets or sets the referenced (parent) table name.
    /// </summary>
    /// <value>The name of the table being referenced.</value>
    public required string ReferencedTable { get; init; }

    /// <summary>
    /// Gets or sets the referenced column name in the parent table.
    /// </summary>
    /// <value>The name of the referenced column (typically the primary key).</value>
    public required string ReferencedColumn { get; init; }

    /// <summary>
    /// Gets or sets the action to take when the referenced row is deleted.
    /// </summary>
    /// <value>The ON DELETE action (default: NoAction).</value>
    public IForeignKeyAction OnDelete { get; init; } = ForeignKeyActions.NoAction;

    /// <summary>
    /// Gets or sets the action to take when the referenced row is updated.
    /// </summary>
    /// <value>The ON UPDATE action (default: NoAction).</value>
    public IForeignKeyAction OnUpdate { get; init; } = ForeignKeyActions.NoAction;

    /// <summary>
    /// Gets or sets the schema of the referenced table.
    /// </summary>
    /// <value>The schema name, or null for default schema.</value>
    public string? ReferencedSchema { get; init; }
}
