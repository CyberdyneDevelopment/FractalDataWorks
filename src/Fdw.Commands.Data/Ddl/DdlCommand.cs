using Fdw.Commands.Data.Abstractions;

namespace Fdw.Commands.Data.Ddl;

/// <summary>
/// Base class for DDL (Data Definition Language) commands.
/// </summary>
/// <remarks>
/// <para>
/// DDL commands perform schema operations: CREATE TABLE, ALTER TABLE, DROP TABLE, CREATE INDEX, etc.
/// Returns bool indicating success/failure of the DDL operation.
/// </para>
/// <para>
/// Unlike DML commands (Query, Insert, Update, Delete), DDL commands modify the database schema
/// and are typically executed during:
/// <list type="bullet">
/// <item>Application startup (EnsureSchema)</item>
/// <item>Database migrations</item>
/// <item>Configuration persistence initialization</item>
/// </list>
/// </para>
/// </remarks>
public abstract class DdlCommand : DataCommandBase<bool>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DdlCommand"/> class.
    /// </summary>
    /// <param name="commandType">The DDL command type.</param>
    /// <param name="tableName">The name of the table this command operates on.</param>
    protected DdlCommand(IDdlCommandType commandType, string tableName)
        : base($"Ddl.{commandType.Name}")
    {
        DdlCommandType = commandType;
        // Why: TableName was previously passed as containerName to DataCommandBase.
        // Addressing is now in DataStoreTarget; the table name is kept as command metadata.
        TableName = tableName;
    }

    /// <summary>
    /// Gets the DDL command type.
    /// </summary>
    /// <value>The type of DDL operation (CreateTable, AlterTable, etc.).</value>
    public IDdlCommandType DdlCommandType { get; }

    /// <summary>
    /// Gets the name of the table this command operates on.
    /// </summary>
    public string TableName { get; }

    /// <summary>
    /// Gets or sets the schema name.
    /// </summary>
    /// <value>The schema name, or null for default schema.</value>
    public string? SchemaName { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to execute the command only if the object doesn't exist.
    /// </summary>
    /// <value>True to add IF NOT EXISTS clause; otherwise, false.</value>
    public bool IfNotExists { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to execute the command only if the object exists.
    /// </summary>
    /// <value>True to add IF EXISTS clause; otherwise, false.</value>
    public bool IfExists { get; init; }
}
