#pragma warning disable CS1591
using System.Collections.Generic;
using System.Linq;
using Fdw.Schema.Ddl.Commands;
using Fdw.Types;

namespace Fdw.Schema.Ddl.Helpers;

/// <summary>
/// Helper class for generating DDL commands from TypeCollection metadata.
/// This can be used to manually generate DDL or integrated into source generators.
/// </summary>
public static class TypeCollectionDdlHelper
{
    private static readonly string[] TypeCollectionColumns = new[]
    {
        "Id",
        "Name",
        "FullName",
        "CollectionKind",
        "ServiceCategory",
        "AssemblyQualifiedName"
    };

    private static readonly string[] TypeOptionColumns = new[]
    {
        "Id",
        "Name",
        "TypeCollectionId",
        "FullTypeName",
        "Category",
        "Description"
    };

    private static readonly string[] TypePropertyColumns = new[]
    {
        "TypeOptionId",
        "Name",
        "PropertyType",
        "PropertyRole",
        "SqlType",
        "MaxLength",
        "IsNullable",
        "IsCollection"
    };

    /// <summary>
    /// Generates DDL INSERT commands for persisting TypeCollection metadata to the database.
    /// </summary>
    /// <param name="metadata">The TypeCollection metadata.</param>
    /// <returns>A list of DDL INSERT commands for types.TypeCollection and types.TypeOption tables.</returns>
#pragma warning disable MA0051 // Sequential data mapping to InsertDataCommands with low cyclomatic complexity
    public static IReadOnlyList<IDdlCommand> GenerateDdlCommands(TypeCollectionMetadata metadata)
    {
        var commands = new List<IDdlCommand>();

        // Insert into types.TypeCollection
        commands.Add(new InsertDataCommand
        {
            SchemaName = "types",
            TableName = "TypeCollection",
            Columns = TypeCollectionColumns,
            Values = new[]
            {
                new object?[]
                {
                    metadata.Id,
                    metadata.Name,
                    metadata.FullName,
                    metadata.CollectionKind.Name, // Assumes ICollectionKind has Name property
                    metadata.ServiceCategory,
                    metadata.AssemblyQualifiedName
                }
            },
            IdentityInsert = false
        });

        // Insert into types.TypeOption for each option
        if (metadata.Options != null && metadata.Options.Count > 0)
        {
            var optionRows = metadata.Options.Select(option => new object?[]
            {
                option.Id,
                option.Name,
                option.TypeCollectionId,
                option.FullTypeName,
                option.Category,
                option.Description
            }).ToList();

            commands.Add(new InsertDataCommand
            {
                SchemaName = "types",
                TableName = "TypeOption",
                Columns = TypeOptionColumns,
                Values = optionRows,
                IdentityInsert = false
            });

            // Insert into types.TypeProperty for properties (if any)
            var propertyRows = new List<object?[]>();

            foreach (var option in metadata.Options)
            {
                if (option.Properties != null && option.Properties.Count > 0)
                {
                    foreach (var property in option.Properties)
                    {
                        propertyRows.Add(new object?[]
                        {
                            option.Id, // TypeOptionId
                            property.Name,
                            property.PropertyType,
                            property.PropertyRole,
                            property.SqlType,
                            property.MaxLength,
                            property.IsNullable,
                            property.IsCollection
                        });
                    }
                }
            }

            if (propertyRows.Count > 0)
            {
                commands.Add(new InsertDataCommand
                {
                    SchemaName = "types",
                    TableName = "TypeProperty",
                    Columns = TypePropertyColumns,
                    Values = propertyRows,
                    IdentityInsert = false
                });
            }
        }

        return commands;
    }
#pragma warning restore MA0051

    /// <summary>
    /// Generates a code snippet for the DdlCommands property that can be added to generated TypeCollections.
    /// This is a helper for manual code generation or future generator enhancements.
    /// </summary>
    /// <param name="collectionFullName">The full name of the TypeCollection class.</param>
    /// <param name="collectionName">The simple name of the TypeCollection class.</param>
    /// <returns>C# code snippet for the DdlCommands property.</returns>
    public static string GenerateDdlCommandsPropertyCode(string collectionFullName, string collectionName)
    {
        return $@"
        private static IReadOnlyList<Fdw.Schema.Ddl.Commands.IDdlCommand>? _ddlCommands;

        /// <summary>
        /// Gets the DDL commands for persisting this TypeCollection metadata to the database.
        /// </summary>
        public static IReadOnlyList<Fdw.Schema.Ddl.Commands.IDdlCommand> DdlCommands =>
            _ddlCommands ??= BuildDdlCommands();

        private static IReadOnlyList<Fdw.Schema.Ddl.Commands.IDdlCommand> BuildDdlCommands()
        {{
            var metadata = GetMetadata();
            return Fdw.Schema.Ddl.Helpers.TypeCollectionDdlHelper.GenerateDdlCommands(metadata);
        }}
";
    }
}
