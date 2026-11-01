namespace Fdw.Configuration.Persistence.Schema;

/// <summary>
/// Represents a foreign key definition for DDL generation.
/// </summary>
public sealed class ForeignKeyDefinition
{
    /// <summary>
    /// Gets or sets the constraint name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Gets or sets the column in this table.
    /// </summary>
    public string Column { get; set; } = "";

    /// <summary>
    /// Gets or sets the referenced schema.
    /// </summary>
    public string ReferencedSchema { get; set; } = "";

    /// <summary>
    /// Gets or sets the referenced table.
    /// </summary>
    public string ReferencedTable { get; set; } = "";

    /// <summary>
    /// Gets or sets the referenced column.
    /// </summary>
    public string ReferencedColumn { get; set; } = "";

    /// <summary>
    /// Gets or sets the ON DELETE action.
    /// </summary>
    public IForeignKeyAction OnDelete { get; set; } = new NoActionForeignKeyAction();

    /// <summary>
    /// Gets or sets the ON UPDATE action.
    /// </summary>
    public IForeignKeyAction OnUpdate { get; set; } = new NoActionForeignKeyAction();
}
