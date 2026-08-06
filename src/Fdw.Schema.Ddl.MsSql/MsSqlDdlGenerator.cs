#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.Schema.Ddl.Commands;
using Fdw.Schema.Ddl.Definitions;
using Fdw.Schema.Ddl.Results;
using Fdw.Schema.Indexes;
using Fdw.Schema.Properties;
using Fdw.Schema.Schemas;
using StringComparison = System.StringComparison;

namespace Fdw.Schema.Ddl.MsSql;

/// <summary>
/// SQL Server implementation of IDdlGenerator.
/// </summary>
public sealed class MsSqlDdlGenerator : IDdlGenerator
{
    /// <inheritdoc/>
    public string TargetDatabase => "MsSql";

    /// <inheritdoc/>
    public IGenericResult<IReadOnlyList<IDdlCommand>> GenerateCommands<TProperty>(
        ISchemaDefinition<TProperty> schema,
        DdlGenerationOptions? options = null)
        where TProperty : IPropertyDefinition
    {
        try
        {
            options ??= new DdlGenerationOptions();
            var commands = new List<IDdlCommand>();

            var columns = ConvertToColumnDefinitions(schema.Properties);
            var primaryKeyColumns = GetPrimaryKeyColumns(schema);
            var schemaName = options.SchemaName;
            var tableName = schema.Name;

            var createTableCommand = new CreateTableCommand
            {
                SchemaName = schemaName,
                TableName = tableName,
                Columns = columns,
                PrimaryKeyName = $"PK_{tableName}",
                PrimaryKeyColumns = primaryKeyColumns.Count > 0 ? primaryKeyColumns : null,
                ForeignKeys = options.IncludeForeignKeys ? ConvertForeignKeys(schema) : null
            };

            commands.Add(createTableCommand);

            if (options.IncludeIndexes && schema.Indexes?.Count > 0)
            {
                commands.AddRange(CreateIndexCommands(schema.Indexes, schemaName, tableName));
            }

            return GenericResult<IReadOnlyList<IDdlCommand>>.Success(commands);
        }
        catch (Exception ex)
        {
            var details = ResultDetails.Create("error", ex.Message);
            return GenericResult<IReadOnlyList<IDdlCommand>>.Failure(
                DdlResultCodes.ByName("CommandGenerationFailed"), details);
        }
    }

    private static List<DdlColumnDefinition> ConvertToColumnDefinitions<TProperty>(IReadOnlyList<TProperty> properties)
        where TProperty : IPropertyDefinition
    {
        var columns = new List<DdlColumnDefinition>();
        foreach (var property in properties)
        {
            var columnDef = new DdlColumnDefinition
            {
                Name = property.Name,
                SqlType = MsSqlTypeMapper.MapToSqlType(property),
                MaxLength = MsSqlTypeMapper.GetMaxLength(property),
                Precision = MsSqlTypeMapper.GetPrecision(property),
                Scale = MsSqlTypeMapper.GetScale(property),
                IsNullable = !property.IsRequired,
                // Why: IsPrimaryKey removed from DdlColumnDefinition — PK identity now in KeyField tables.
                IsUnique = false,
                DefaultValue = GetDefaultValueFromMetadata(property.Metadata)
            };

            columns.Add(columnDef);
        }
        return columns;
    }

    private static List<string> GetPrimaryKeyColumns<TProperty>(ISchemaDefinition<TProperty> schema)
        where TProperty : IPropertyDefinition
    {
        var primaryKeyColumns = new List<string>();
        if (schema.SurrogateKey != null)
        {
            primaryKeyColumns.AddRange(schema.SurrogateKey.Members.Select(m => m.PropertyName));
        }
        else if (schema.NaturalKey != null)
        {
            primaryKeyColumns.AddRange(schema.NaturalKey.Members.Select(m => m.PropertyName));
        }
        return primaryKeyColumns;
    }

    private static List<CreateIndexCommand> CreateIndexCommands<TProperty>(IReadOnlyList<IIndexDefinition<TProperty>> indexes, string schemaName, string tableName)
        where TProperty : IPropertyDefinition
    {
        var commands = new List<CreateIndexCommand>();
        foreach (var indexDef in indexes)
        {
            var ddlIndexDef = new DdlIndexDefinition
            {
                Name = indexDef.Name,
                Columns = indexDef.Members.Select(m => m.PropertyName).ToList(),
                IsUnique = indexDef.IsUnique,
                IsClustered = indexDef.IsClustered,
                IncludeColumns = indexDef.IncludeColumns,
                FilterPredicate = indexDef.FilterPredicate
            };

            var indexCommand = new CreateIndexCommand
            {
                SchemaName = schemaName,
                TableName = tableName,
                IndexName = indexDef.Name,
                Definition = ddlIndexDef
            };

            commands.Add(indexCommand);
        }
        return commands;
    }

    private static string? GetDefaultValueFromMetadata(IReadOnlyDictionary<string, object>? metadata)
    {
        if (metadata == null)
            return null;

        if (metadata.TryGetValue("DefaultValue", out var value))
        {
            return value?.ToString();
        }

        return null;
    }

    private static IReadOnlyList<DdlForeignKeyDefinition>? ConvertForeignKeys<TProperty>(ISchemaDefinition<TProperty> schema)
        where TProperty : IPropertyDefinition
    {
        // Foreign keys would come from metadata or specialized schema properties
        // For now, return null as this is provider-specific
        return null;
    }

    /// <inheritdoc/>
    public IGenericResult<string> GenerateSql(IDdlCommand command)
    {
        try
        {
            var sql = command switch
            {
                CreateSchemaCommand cs => GenerateCreateSchema(cs),
                CreateTableCommand ct => GenerateCreateTable(ct),
                CreateIndexCommand ci => GenerateCreateIndex(ci),
                DropTableCommand dt => GenerateDropTable(dt),
                DropIndexCommand di => GenerateDropIndex(di),
                DropSchemaCommand ds => GenerateDropSchema(ds),
                _ => throw new NotSupportedException($"Command type {command.CommandType} is not supported")
            };

            return GenericResult<string>.Success(sql);
        }
        catch (Exception ex)
        {
            var details = ResultDetails.Create("commandType", command.CommandType.ToString(), "error", ex.Message);
            return GenericResult<string>.Failure(
                DdlResultCodes.ByName("CommandGenerationFailed"), details);
        }
    }

    /// <inheritdoc/>
    public IGenericResult<string> GenerateScript(IReadOnlyList<IDdlCommand> commands)
    {
        try
        {
            var sb = new StringBuilder();

            foreach (var command in commands)
            {
                var sqlResult = GenerateSql(command);
                if (!sqlResult.IsSuccess)
                    return sqlResult;

                if (sb.Length > 0)
                    sb.AppendLine();

                sb.AppendLine(sqlResult.Value);
            }

            return GenericResult<string>.Success(sb.ToString());
        }
        catch (Exception ex)
        {
            var details = ResultDetails.Create("error", ex.Message);
            return GenericResult<string>.Failure(
                DdlResultCodes.ByName("CommandGenerationFailed"), details);
        }
    }

    private static string GenerateCreateSchema(CreateSchemaCommand command)
    {
        var sb = new StringBuilder();

        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = '{command.SchemaName}')");
        sb.AppendLine("BEGIN");
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    EXEC('CREATE SCHEMA [{command.SchemaName}]')");
        sb.AppendLine("END");
        sb.Append("GO");

        return sb.ToString();
    }

    private static string GenerateCreateTable(CreateTableCommand command)
    {
        var sb = new StringBuilder();
        var fullTableName = string.IsNullOrWhiteSpace(command.SchemaName)
            ? command.TableName
            : $"{command.SchemaName}.{command.TableName}";

        // IF NOT EXISTS check
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'{fullTableName}') AND type = 'U')");
        sb.AppendLine("BEGIN");
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    CREATE TABLE {fullTableName}");
        sb.AppendLine("    (");

        // Column definitions
        var columnLines = BuildColumnLines(command.Columns);
        foreach (var line in columnLines)
        {
            sb.AppendLine(line);
        }

        // Constraints
        var constraints = BuildConstraints(command);
        foreach (var constraint in constraints)
        {
            sb.AppendLine();
            sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    ,{constraint}");
        }

        sb.AppendLine("    )");
        sb.AppendLine("END");
        sb.Append("GO");

        return sb.ToString();
    }

    private static List<string> BuildColumnLines(IReadOnlyList<DdlColumnDefinition> columns)
    {
        var columnLines = new List<string>();
        var maxNameLength = columns.Max(c => c.Name.Length);
        var maxTypeLength = columns.Max(c => c.GetFullSqlType().Length);

        // Align columns to at least 20 chars, types to 20 chars
        var nameWidth = Math.Max(20, maxNameLength + 1);
        var typeWidth = Math.Max(20, maxTypeLength + 1);

        for (int i = 0; i < columns.Count; i++)
        {
            var column = columns[i];
            var line = new StringBuilder();

            // Comma-first formatting (first line starts with space, others with comma)
            line.Append(i == 0 ? "     " : "    ,");

            // Column name (left-aligned)
            line.Append(column.Name.PadRight(nameWidth));

            // SQL type (left-aligned)
            line.Append(column.GetFullSqlType().PadRight(typeWidth));

            // Nullability
            line.Append(column.IsNullable ? "NULL        " : "NOT NULL    ");

            // Default value
            if (!string.IsNullOrWhiteSpace(column.DefaultValue))
            {
                line.Append(System.Globalization.CultureInfo.InvariantCulture, $"DEFAULT {column.DefaultValue}");
            }

            columnLines.Add(line.ToString());
        }

        return columnLines;
    }

    private static List<string> BuildConstraints(CreateTableCommand command)
    {
        var constraints = new List<string>();

        // Primary key constraint
        if (command.PrimaryKeyColumns?.Count > 0)
        {
            var pkColumns = string.Join(", ", command.PrimaryKeyColumns);
            var pkName = command.PrimaryKeyName ?? $"PK_{command.TableName}";
            constraints.Add($"CONSTRAINT {pkName} PRIMARY KEY ({pkColumns})");
        }

        // Unique constraints
        // Why: IsPrimaryKey removed from DdlColumnDefinition — filter only on IsUnique.
        var uniqueColumns = command.Columns.Where(c => c.IsUnique).ToList();
        foreach (var column in uniqueColumns)
        {
            constraints.Add($"CONSTRAINT UQ_{command.TableName}_{column.Name} UNIQUE ({column.Name})");
        }

        // Foreign key constraints
        if (command.ForeignKeys?.Count > 0)
        {
            foreach (var fk in command.ForeignKeys)
            {
                var fkDef = new StringBuilder();
                fkDef.Append(System.Globalization.CultureInfo.InvariantCulture, $"CONSTRAINT {fk.Name} FOREIGN KEY ({fk.ColumnName}) ");
                fkDef.Append(System.Globalization.CultureInfo.InvariantCulture, $"REFERENCES {fk.ReferencedSchema}.{fk.ReferencedTable}({fk.ReferencedColumn})");

                if (!string.Equals(fk.OnDelete.Name, "NoAction", StringComparison.Ordinal))
                {
                    fkDef.Append(System.Globalization.CultureInfo.InvariantCulture, $" ON DELETE {GetForeignKeyAction(fk.OnDelete)}");
                }

                if (!string.Equals(fk.OnUpdate.Name, "NoAction", StringComparison.Ordinal))
                {
                    fkDef.Append(System.Globalization.CultureInfo.InvariantCulture, $" ON UPDATE {GetForeignKeyAction(fk.OnUpdate)}");
                }

                constraints.Add(fkDef.ToString());
            }
        }

        return constraints;
    }

    private static string GenerateCreateIndex(CreateIndexCommand command)
    {
        var sb = new StringBuilder();
        var fullTableName = string.IsNullOrWhiteSpace(command.SchemaName)
            ? command.TableName
            : $"{command.SchemaName}.{command.TableName}";

        var indexType = command.Definition.IsUnique ? "UNIQUE " : "";
        var clustered = command.Definition.IsClustered ? "CLUSTERED " : "NONCLUSTERED ";
        var columns = string.Join(", ", command.Definition.Columns);

        sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"CREATE {indexType}{clustered}INDEX {command.IndexName} ON {fullTableName} ({columns})");

        if (command.Definition.IncludeColumns?.Count > 0)
        {
            var includeColumns = string.Join(", ", command.Definition.IncludeColumns);
            sb.Append(System.Globalization.CultureInfo.InvariantCulture, $" INCLUDE ({includeColumns})");
        }

        if (!string.IsNullOrWhiteSpace(command.Definition.FilterPredicate))
        {
            sb.Append(System.Globalization.CultureInfo.InvariantCulture, $" WHERE {command.Definition.FilterPredicate}");
        }

        if (command.Definition.FillFactor.HasValue)
        {
            sb.Append(System.Globalization.CultureInfo.InvariantCulture, $" WITH (FILLFACTOR = {command.Definition.FillFactor.Value})");
        }

        sb.AppendLine();
        sb.Append("GO");

        return sb.ToString();
    }

    private static string GenerateDropTable(DropTableCommand command)
    {
        var sb = new StringBuilder();
        var fullTableName = string.IsNullOrWhiteSpace(command.SchemaName)
            ? command.TableName
            : $"{command.SchemaName}.{command.TableName}";

        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'{fullTableName}') AND type = 'U')");
        sb.AppendLine("BEGIN");
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    DROP TABLE {fullTableName}");
        sb.AppendLine("END");
        sb.Append("GO");

        return sb.ToString();
    }

    private static string GenerateDropIndex(DropIndexCommand command)
    {
        var sb = new StringBuilder();
        var fullTableName = string.IsNullOrWhiteSpace(command.SchemaName)
            ? command.TableName
            : $"{command.SchemaName}.{command.TableName}";

        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = '{command.IndexName}' AND object_id = OBJECT_ID(N'{fullTableName}'))");
        sb.AppendLine("BEGIN");
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    DROP INDEX {command.IndexName} ON {fullTableName}");
        sb.AppendLine("END");
        sb.Append("GO");

        return sb.ToString();
    }

    private static string GenerateDropSchema(DropSchemaCommand command)
    {
        var sb = new StringBuilder();

        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"IF EXISTS (SELECT 1 FROM sys.schemas WHERE name = '{command.SchemaName}')");
        sb.AppendLine("BEGIN");
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    DROP SCHEMA [{command.SchemaName}]");
        sb.AppendLine("END");
        sb.Append("GO");

        return sb.ToString();
    }

    private static string GetForeignKeyAction(IDdlForeignKeyAction action)
    {
        return action.Name switch
        {
            "Cascade" => "CASCADE",
            "SetNull" => "SET NULL",
            "SetDefault" => "SET DEFAULT",
            "NoAction" => "NO ACTION",
            _ => "NO ACTION"
        };
    }
}
