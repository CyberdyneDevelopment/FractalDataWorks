#pragma warning disable CS1591
using Fdw.Schema.Ddl.Definitions;

namespace Fdw.Schema.Ddl.Commands;

/// <summary>
/// DDL command for creating a database index.
/// </summary>
public sealed class CreateIndexCommand : IDdlCommand
{
    /// <inheritdoc/>
    public IDdlCommandType CommandType { get; } = new CreateIndexDdlCommandType();

    /// <inheritdoc/>
    public string? SchemaName { get; init; }

    /// <inheritdoc/>
    public string ObjectName => IndexName;

    /// <summary>
    /// Gets or sets the index name.
    /// </summary>
    public required string IndexName { get; init; }

    /// <summary>
    /// Gets or sets the table name the index is created on.
    /// </summary>
    public required string TableName { get; init; }

    /// <summary>
    /// Gets or sets the index definition.
    /// </summary>
    public required DdlIndexDefinition Definition { get; init; }
}
