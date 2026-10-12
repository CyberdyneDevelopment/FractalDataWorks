#pragma warning disable CS1591
namespace Fdw.Schema.Ddl.Commands;

/// <summary>
/// DDL command for dropping a database schema.
/// </summary>
public sealed class DropSchemaCommand : IDdlCommand
{
    /// <inheritdoc/>
    public IDdlCommandType CommandType { get; } = new DropSchemaDdlCommandType();

    /// <inheritdoc/>
    public string? SchemaName => Name;

    /// <inheritdoc/>
    public string ObjectName => Name;

    /// <summary>
    /// Gets or sets the schema name to drop.
    /// </summary>
    public required string Name { get; init; }
}
