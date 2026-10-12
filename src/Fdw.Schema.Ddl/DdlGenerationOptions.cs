#pragma warning disable CS1591
namespace Fdw.Schema.Ddl;

/// <summary>
/// Options for controlling DDL generation behavior.
/// </summary>
public sealed class DdlGenerationOptions
{
    /// <summary>
    /// Gets or sets the schema name to use (default: "dbo").
    /// </summary>
    public string SchemaName { get; init; } = "dbo";

    /// <summary>
    /// Gets or sets whether to generate "IF NOT EXISTS" checks (default: true).
    /// </summary>
    public bool IfNotExists { get; init; } = true;

    /// <summary>
    /// Gets or sets whether to include index creation statements (default: true).
    /// </summary>
    public bool IncludeIndexes { get; init; } = true;

    /// <summary>
    /// Gets or sets whether to include foreign key constraints (default: true).
    /// </summary>
    public bool IncludeForeignKeys { get; init; } = true;

    /// <summary>
    /// Gets or sets whether to include DROP statements before CREATE (default: false).
    /// </summary>
    public bool IncludeDropStatements { get; init; }
}
