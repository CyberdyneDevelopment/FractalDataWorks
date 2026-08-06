#pragma warning disable CS1591
using System.Collections.Generic;

namespace Fdw.Schema.Ddl.Commands;

/// <summary>
/// DDL command for generating INSERT statements to populate data.
/// Used for inserting metadata and lookup values into tables.
/// </summary>
public sealed class InsertDataCommand : IDdlCommand
{
    /// <inheritdoc/>
    public IDdlCommandType CommandType { get; } = new InsertDataDdlCommandType();

    /// <inheritdoc/>
    public string? SchemaName { get; init; }

    /// <inheritdoc/>
    public string ObjectName => TableName;

    /// <summary>
    /// Gets or sets the table name.
    /// </summary>
    public required string TableName { get; init; }

    /// <summary>
    /// Gets or sets the column names.
    /// </summary>
    public required IReadOnlyList<string> Columns { get; init; }

    /// <summary>
    /// Gets or sets the rows of values to insert.
    /// Each row must have the same number of values as Columns.
    /// </summary>
    public required IReadOnlyList<IReadOnlyList<object?>> Values { get; init; }

    /// <summary>
    /// Gets or sets whether to generate an identity insert wrapper (SET IDENTITY_INSERT ON/OFF).
    /// </summary>
    public bool IdentityInsert { get; init; }
}
