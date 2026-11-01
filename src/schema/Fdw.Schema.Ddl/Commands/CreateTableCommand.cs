#pragma warning disable CS1591
using System.Collections.Generic;
using Fdw.Schema.Ddl.Definitions;

namespace Fdw.Schema.Ddl.Commands;

/// <summary>
/// DDL command for creating a database table.
/// </summary>
public sealed class CreateTableCommand : IDdlCommand
{
    /// <inheritdoc/>
    public IDdlCommandType CommandType { get; } = new CreateTableDdlCommandType();

    /// <inheritdoc/>
    public string? SchemaName { get; init; }

    /// <inheritdoc/>
    public string ObjectName => TableName;

    /// <summary>
    /// Gets or sets the table name.
    /// </summary>
    public required string TableName { get; init; }

    /// <summary>
    /// Gets or sets the column definitions.
    /// </summary>
    public required IReadOnlyList<DdlColumnDefinition> Columns { get; init; }

    /// <summary>
    /// Gets or sets the index definitions.
    /// </summary>
    public IReadOnlyList<DdlIndexDefinition>? Indexes { get; init; }

    /// <summary>
    /// Gets or sets the foreign key constraints.
    /// </summary>
    public IReadOnlyList<DdlForeignKeyDefinition>? ForeignKeys { get; init; }

    /// <summary>
    /// Gets or sets the primary key constraint name.
    /// </summary>
    public string? PrimaryKeyName { get; init; }

    /// <summary>
    /// Gets or sets the primary key column names.
    /// </summary>
    public IReadOnlyList<string>? PrimaryKeyColumns { get; init; }
}
