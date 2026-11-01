namespace Fdw.Schema.Ddl.Definitions;

/// <summary>
/// Defines a foreign key constraint for DDL generation.
/// </summary>
public sealed class DdlForeignKeyDefinition
{
    /// <summary>
    /// Gets or sets the foreign key constraint name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the column name in the current table.
    /// </summary>
    public required string ColumnName { get; init; }

    /// <summary>
    /// Gets or sets the referenced schema.
    /// </summary>
    public required string ReferencedSchema { get; init; }

    /// <summary>
    /// Gets or sets the referenced table name.
    /// </summary>
    public required string ReferencedTable { get; init; }

    /// <summary>
    /// Gets or sets the referenced column name.
    /// </summary>
    public required string ReferencedColumn { get; init; }

    /// <summary>
    /// Gets or sets the action to take on delete.
    /// </summary>
    public IDdlForeignKeyAction OnDelete { get; init; } = new NoActionDdlForeignKeyAction();

    /// <summary>
    /// Gets or sets the action to take on update.
    /// </summary>
    public IDdlForeignKeyAction OnUpdate { get; init; } = new NoActionDdlForeignKeyAction();
}
