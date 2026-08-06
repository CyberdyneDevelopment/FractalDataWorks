#pragma warning disable CS1591
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Schema.Ddl.Commands;
using Fdw.Schema.Ddl.Results;
using Fdw.Schema.Properties;
using Fdw.Schema.Schemas;

namespace Fdw.Schema.Ddl.Extensions;

/// <summary>
/// Extension methods for generating DDL from schema definitions.
/// </summary>
public static class SchemaDefinitionDdlExtensions
{
    /// <summary>
    /// Generates DDL commands from a schema definition.
    /// </summary>
    /// <typeparam name="TProperty">The property definition type.</typeparam>
    /// <param name="schema">The schema definition.</param>
    /// <param name="generator">The DDL generator to use.</param>
    /// <param name="options">Optional generation options.</param>
    /// <returns>A result containing the list of DDL commands, or an error.</returns>
    public static IGenericResult<IReadOnlyList<IDdlCommand>> ToDdlCommands<TProperty>(
        this ISchemaDefinition<TProperty> schema,
        IDdlGenerator generator,
        DdlGenerationOptions? options = null)
        where TProperty : IPropertyDefinition
    {
        return generator.GenerateCommands(schema, options);
    }

    /// <summary>
    /// Generates a SQL script from a schema definition.
    /// </summary>
    /// <typeparam name="TProperty">The property definition type.</typeparam>
    /// <param name="schema">The schema definition.</param>
    /// <param name="generator">The DDL generator to use.</param>
    /// <param name="options">Optional generation options.</param>
    /// <returns>A result containing the SQL script text, or an error.</returns>
    public static IGenericResult<string> ToSqlScript<TProperty>(
        this ISchemaDefinition<TProperty> schema,
        IDdlGenerator generator,
        DdlGenerationOptions? options = null)
        where TProperty : IPropertyDefinition
    {
        var commandsResult = generator.GenerateCommands(schema, options);
        if (!commandsResult.IsSuccess)
        {
            return commandsResult.Messages.Any()
                ? commandsResult.ToNewResult<string>()
                : GenericResult<string>.Failure(DdlResultCodes.ByName("CommandGenerationFailed"));
        }

        if (commandsResult.Value == null || commandsResult.Value.Count == 0)
        {
            return GenericResult<string>.Failure(DdlResultCodes.ByName("NoCommandsGenerated"));
        }

        return generator.GenerateScript(commandsResult.Value);
    }
}
