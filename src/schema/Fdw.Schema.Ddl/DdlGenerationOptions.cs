#pragma warning disable CS1591
namespace Fdw.Schema.Ddl;

/// <summary>
/// Options for controlling DDL generation behavior.
/// </summary>
/// <remarks>Every member is required (FDW-3823): the caller that asks for DDL states each, and no value is taken in its place.</remarks>
public sealed class DdlGenerationOptions
{
    /// <summary>
    /// Gets or sets the schema name to use.
    /// </summary>
    public required string SchemaName { get; init; }

    /// <summary>
    /// Gets or sets whether to generate "IF NOT EXISTS" checks.
    /// </summary>
    public required bool IfNotExists { get; init; }

    /// <summary>
    /// Gets or sets whether to include index creation statements.
    /// </summary>
    public required bool IncludeIndexes { get; init; }

    /// <summary>
    /// Gets or sets whether to include foreign key constraints.
    /// </summary>
    public required bool IncludeForeignKeys { get; init; }

    /// <summary>
    /// Gets or sets whether to include DROP statements before CREATE.
    /// </summary>
    public required bool IncludeDropStatements { get; init; }
}
