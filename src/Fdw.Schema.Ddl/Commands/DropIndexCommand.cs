#pragma warning disable CS1591
namespace Fdw.Schema.Ddl.Commands;

/// <summary>
/// DDL command for dropping a database index.
/// </summary>
public sealed class DropIndexCommand : IDdlCommand
{
    /// <inheritdoc/>
    public IDdlCommandType CommandType { get; } = new DropIndexDdlCommandType();

    /// <inheritdoc/>
    public string? SchemaName { get; init; }

    /// <inheritdoc/>
    public string ObjectName => IndexName;

    /// <summary>
    /// Gets or sets the index name to drop.
    /// </summary>
    public required string IndexName { get; init; }

    /// <summary>
    /// Gets or sets the table name the index is on.
    /// </summary>
    public required string TableName { get; init; }
}
