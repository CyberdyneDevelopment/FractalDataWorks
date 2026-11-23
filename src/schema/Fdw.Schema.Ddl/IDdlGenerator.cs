#pragma warning disable CS1591
using System.Collections.Generic;
using Fdw.Results;
using Fdw.Schema.Ddl.Commands;
using Fdw.Schema.Properties;
using Fdw.Schema.Schemas;

namespace Fdw.Schema.Ddl;

/// <summary>
/// Interface for generating DDL commands and SQL scripts from schema definitions.
/// </summary>
public interface IDdlGenerator
{
    /// <summary>
    /// Gets the target database system (e.g., "MsSql", "PostgreSql", "MySql").
    /// </summary>
    string TargetDatabase { get; }

    /// <summary>
    /// Quotes one identifier the way this engine's DDL needs it, so that a name is written as that name and never as more SQL (FDW-3617).
    /// </summary>
    /// <param name="identifier">The name of a schema, table, column, constraint or index.</param>
    /// <returns>The quoted name, or the coded refusal <c>IdentifierInvalid</c> for a name the engine cannot be given (empty, or holding a NUL).</returns>
    IGenericResult<string> QuoteIdentifier(string identifier);

    /// <summary>
    /// Generates DDL commands from a schema definition.
    /// </summary>
    /// <typeparam name="TProperty">The property definition type.</typeparam>
    /// <param name="schema">The schema definition to generate commands from.</param>
    /// <param name="options">The generation options; the caller states every one.</param>
    /// <returns>A result containing the list of DDL commands, or an error.</returns>
    IGenericResult<IReadOnlyList<IDdlCommand>> GenerateCommands<TProperty>(
        ISchemaDefinition<TProperty> schema,
        DdlGenerationOptions options)
        where TProperty : IPropertyDefinition;

    /// <summary>
    /// Generates SQL text for a single DDL command.
    /// </summary>
    /// <param name="command">The DDL command to generate SQL for.</param>
    /// <returns>A result containing the SQL text, or an error.</returns>
    IGenericResult<string> GenerateSql(IDdlCommand command);

    /// <summary>
    /// Generates a complete SQL script from multiple DDL commands.
    /// </summary>
    /// <param name="commands">The DDL commands to generate a script for.</param>
    /// <returns>A result containing the SQL script text, or an error.</returns>
    IGenericResult<string> GenerateScript(IReadOnlyList<IDdlCommand> commands);
}
