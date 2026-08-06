#pragma warning disable CS1591
namespace Fdw.Schema.Ddl.Commands;

/// <summary>
/// DDL command for creating a database schema.
/// </summary>
public sealed class CreateSchemaCommand : IDdlCommand
{
    /// <inheritdoc/>
    public IDdlCommandType CommandType { get; } = new CreateSchemaDdlCommandType();

    /// <inheritdoc/>
    public string? SchemaName => Name;

    /// <inheritdoc/>
    public string ObjectName => Name;

    /// <summary>
    /// Gets or sets the schema name to create.
    /// </summary>
    public required string Name { get; init; }
}
