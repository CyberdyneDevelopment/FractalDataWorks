#pragma warning disable CS1591
namespace Fdw.Schema.Ddl.Commands;

/// <summary>
/// DDL command for dropping a database table.
/// </summary>
public sealed class DropTableCommand : IDdlCommand
{
    /// <inheritdoc/>
    public IDdlCommandType CommandType { get; } = new DropTableDdlCommandType();

    /// <inheritdoc/>
    public string? SchemaName { get; init; }

    /// <inheritdoc/>
    public string ObjectName => TableName;

    /// <summary>
    /// Gets or sets the table name to drop.
    /// </summary>
    public required string TableName { get; init; }
}
