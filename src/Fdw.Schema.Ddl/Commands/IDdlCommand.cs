#pragma warning disable CS1591
namespace Fdw.Schema.Ddl.Commands;

/// <summary>
/// Base interface for all DDL commands.
/// </summary>
public interface IDdlCommand
{
    /// <summary>
    /// Gets the type of DDL command.
    /// </summary>
    IDdlCommandType CommandType { get; }

    /// <summary>
    /// Gets the schema name (if applicable).
    /// </summary>
    string? SchemaName { get; }

    /// <summary>
    /// Gets the object name (table, index, view, etc.).
    /// </summary>
    string ObjectName { get; }
}
