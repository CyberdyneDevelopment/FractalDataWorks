#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fdw.Results;
using Fdw.Configuration.Persistence.Schema;
using Fdw.Results.Abstractions;
using Fdw.Schema.Ddl.Commands;
using Fdw.Schema.Ddl.Definitions;
using Fdw.Schema.Ddl.MsSql.CommandHandlers;
using Fdw.Schema.Ddl.Results;
using Fdw.Schema.Indexes;
using Fdw.Schema.Properties;
using Fdw.Schema.Schemas;

namespace Fdw.Schema.Ddl.MsSql;

/// <summary>
/// SQL Server implementation of IDdlGenerator.
/// </summary>
public sealed class MsSqlDdlGenerator : IDdlGenerator
{
    private const string Dialect = "SQL Server";

    /// <inheritdoc/>
    public string TargetDatabase => "MsSql";

    /// <inheritdoc/>
    public IGenericResult<IReadOnlyList<IDdlCommand>> GenerateCommands<TProperty>(
        ISchemaDefinition<TProperty> schema,
        DdlGenerationOptions options)
        where TProperty : IPropertyDefinition
    {
        try
        {
            var generation = options;
            var commands = new List<IDdlCommand>();

            var columns = ConvertToColumnDefinitions(schema.Properties);
            var primaryKeyColumns = GetPrimaryKeyColumns(schema);
            var schemaName = generation.SchemaName;
            var tableName = schema.Name;

            var createTableCommand = new CreateTableCommand
            {
                SchemaName = schemaName,
                TableName = tableName,
                Columns = columns,
                PrimaryKeyName = $"PK_{tableName}",
                PrimaryKeyColumns = primaryKeyColumns.Count > 0 ? primaryKeyColumns : null,
                ForeignKeys = generation.IncludeForeignKeys ? ConvertForeignKeys(schema) : null
            };

            commands.Add(createTableCommand);

            if (generation.IncludeIndexes && schema.Indexes?.Count > 0)
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
    public IGenericResult<string> QuoteIdentifier(string identifier)
        => WhyInvalid(identifier) is { } reason
            ? InvalidIdentifier(identifier, reason)
            : GenericResult<string>.Success(Quote(identifier));

    /// <inheritdoc/>
    public IGenericResult<string> GenerateSql(IDdlCommand command)
    {
        // Why refused here: the constraint name is the command's to give, and the generator invents none (FDW-3580).
        if (command is CreateTableCommand { PrimaryKeyColumns.Count: > 0, PrimaryKeyName: null or "" } unnamed)
        {
            return GenericResult<string>.Failure(
                DdlResultCodes.ByName("PrimaryKeyNameMissing"),
                ResultDetails.Create("tableName", unnamed.TableName));
        }

        // A command type with no handler (AlterTable, InsertData, CreateView, DropView) is refused as every command this dialect cannot write is.
        var handler = MsSqlDdlCommandHandlers.ByName(command.CommandType.Name);
        if (ReferenceEquals(handler, MsSqlDdlCommandHandlers.NotFound))
            return GenerationFailed(command, $"Command type {command.CommandType} is not supported");

        // Why before any text is written (FDW-3617): a name this dialect cannot quote is refused with its own code, naming the name and
        // why, so a caller reads what to change; it is never written into SQL, and never answered as a generic generation failure.
        if (RefuseInvalidName(handler.NamesIn(command)) is { } invalidName)
            return invalidName;

        try
        {
            // A table can be refused (a foreign key action SQL Server cannot express), so its handler answers a result.
            return handler.Generate(command);
        }
        catch (Exception ex)
        {
            return GenerationFailed(command, ex.Message);
        }
    }

    private static IGenericResult<string> GenerationFailed(IDdlCommand command, string error)
        => GenericResult<string>.Failure(
            DdlResultCodes.ByName("CommandGenerationFailed"),
            ResultDetails.Create("commandType", command.CommandType.ToString(), "error", error));

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

                // Why the separator is here and not in a statement: GO is the batch separator of the tools a person runs a script with, not
                // T-SQL, and SqlCommand refuses it; a statement is what the engine accepts, so the script adds the separator (FDW-3617).
                sb.AppendLine(sqlResult.Value);
                sb.AppendLine("GO");
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

    // The refusal for the first name the command writes that the dialect cannot quote, or null when every one can be.
    private static IGenericResult<string>? RefuseInvalidName(IEnumerable<string?> names)
    {
        foreach (var name in names)
        {
            if (WhyInvalid(name) is { } reason)
                return InvalidIdentifier(name ?? string.Empty, reason);
        }

        return null;
    }

    // Why these two refusals: an empty or blank name is no name, and a NUL ends a name early in the engine's own parsing, so what ran
    // would not be what was asked. Every other character, ] and ' included, is escaped and is that literal name.
    private static string? WhyInvalid(string? identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return "the name is empty";

        return identifier.Contains('\0', StringComparison.Ordinal) ? "the name contains a NUL character" : null;
    }

    private static IGenericResult<string> InvalidIdentifier(string identifier, string reason)
        => GenericResult<string>.Failure(
            DdlResultCodes.ByName("IdentifierInvalid"),
            ResultDetails.Create("identifier", identifier, "reason", reason).With("dialect", Dialect));

    // One name in brackets, a closing bracket doubled: the quoting every identifier this dialect writes goes through.
    private static string Quote(string identifier)
        => string.Concat("[", identifier.Replace("]", "]]", StringComparison.Ordinal), "]");

    // One text inside a string literal, a quote doubled; for the existence checks and the statement EXEC runs.
    private static string Literal(string text) => text.Replace("'", "''", StringComparison.Ordinal);

    private static string Qualified(string? schemaName, string objectName)
        => string.IsNullOrWhiteSpace(schemaName)
            ? Quote(objectName)
            : string.Concat(Quote(schemaName), ".", Quote(objectName));

    private static string QuotedList(IEnumerable<string> names) => string.Join(", ", names.Select(Quote));

    // A schema name the command leaves out (null or blank) names no schema, and is not a name to check.
    internal static IEnumerable<string> Stated(string? schemaName)
        => string.IsNullOrWhiteSpace(schemaName) ? [] : [schemaName];

    internal static IEnumerable<string?> TableNames(CreateTableCommand table)
    {
        foreach (var schema in Stated(table.SchemaName))
            yield return schema;

        yield return table.TableName;

        foreach (var column in table.Columns)
            yield return column.Name;

        // The key's name and columns are written only when the table has a key.
        if (table.PrimaryKeyColumns?.Count > 0)
        {
            yield return table.PrimaryKeyName;
            foreach (var column in table.PrimaryKeyColumns)
                yield return column;
        }

        foreach (var fk in table.ForeignKeys ?? [])
        {
            yield return fk.Name;
            yield return fk.ColumnName;
            foreach (var schema in Stated(fk.ReferencedSchema))
                yield return schema;

            yield return fk.ReferencedTable;
            yield return fk.ReferencedColumn;
        }
    }

    internal static string GenerateCreateSchema(CreateSchemaCommand command)
    {
        var sb = new StringBuilder();

        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'{Literal(command.Name)}')");
        sb.AppendLine("BEGIN");
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    EXEC(N'CREATE SCHEMA {Literal(Quote(command.Name))}')");
        sb.Append("END");

        return sb.ToString();
    }

    internal static IGenericResult<string> GenerateCreateTable(CreateTableCommand command)
    {
        var constraints = BuildConstraints(command);
        if (!constraints.IsSuccess || constraints.Value is null)
            return constraints.ToNewResult<string>();

        var sb = new StringBuilder();
        var fullTableName = Qualified(command.SchemaName, command.TableName);

        // IF NOT EXISTS check
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'{Literal(fullTableName)}') AND type = 'U')");
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
        foreach (var constraint in constraints.Value)
        {
            sb.AppendLine();
            sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    ,{constraint}");
        }

        sb.AppendLine("    )");
        sb.Append("END");

        return GenericResult<string>.Success(sb.ToString());
    }

    // The type is written as the column states it: a discovered type, or one the caller took from SqlColumnTypes; the dialect maps
    // it, and IDENTITY is the engine's own (seed and increment are the engine's, 1 and 1, when none is stated).
    private static string TypeText(DdlColumnDefinition column)
        => column.IsIdentity ? $"{column.GetFullSqlType()} IDENTITY" : column.GetFullSqlType();

    private static List<string> BuildColumnLines(IReadOnlyList<DdlColumnDefinition> columns)
    {
        var columnLines = new List<string>();
        var maxNameLength = columns.Max(c => Quote(c.Name).Length);
        var maxTypeLength = columns.Max(c => TypeText(c).Length);

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
            line.Append(Quote(column.Name).PadRight(nameWidth));

            // SQL type (left-aligned)
            line.Append(TypeText(column).PadRight(typeWidth));

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

    private static IGenericResult<List<string>> BuildConstraints(CreateTableCommand command)
    {
        var constraints = new List<string>();

        // Primary key constraint
        if (command.PrimaryKeyColumns?.Count > 0)
            constraints.Add($"CONSTRAINT {Quote(command.PrimaryKeyName!)} PRIMARY KEY ({QuotedList(command.PrimaryKeyColumns)})");

        // Unique constraints
        var uniqueColumns = command.Columns.Where(c => c.IsUnique).ToList();
        foreach (var column in uniqueColumns)
        {
            constraints.Add($"CONSTRAINT {Quote($"UQ_{command.TableName}_{column.Name}")} UNIQUE ({Quote(column.Name)})");
        }

        // Foreign key constraints
        if (command.ForeignKeys?.Count > 0)
        {
            foreach (var fk in command.ForeignKeys)
            {
                var onDelete = ForeignKeyActionClause(fk, "ON DELETE", fk.OnDelete);
                if (!onDelete.IsSuccess || onDelete.Value is null) return onDelete.ToNewResult<List<string>>();
                var onUpdate = ForeignKeyActionClause(fk, "ON UPDATE", fk.OnUpdate);
                if (!onUpdate.IsSuccess || onUpdate.Value is null) return onUpdate.ToNewResult<List<string>>();

                constraints.Add(
                    $"CONSTRAINT {Quote(fk.Name)} FOREIGN KEY ({Quote(fk.ColumnName)}) " +
                    $"REFERENCES {Qualified(fk.ReferencedSchema, fk.ReferencedTable)}({Quote(fk.ReferencedColumn)})" +
                    onDelete.Value + onUpdate.Value);
            }
        }

        return GenericResult<List<string>>.Success(constraints);
    }

    // The filter predicate and a column default are expressions the definition states, written as stated: only the names are quoted.
    internal static string GenerateCreateIndex(CreateIndexCommand command)
    {
        var sb = new StringBuilder();
        var fullTableName = Qualified(command.SchemaName, command.TableName);

        var indexType = command.Definition.IsUnique ? "UNIQUE " : "";
        var clustered = command.Definition.IsClustered ? "CLUSTERED " : "NONCLUSTERED ";

        sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"CREATE {indexType}{clustered}INDEX {Quote(command.IndexName)} ON {fullTableName} ({QuotedList(command.Definition.Columns)})");

        if (command.Definition.IncludeColumns?.Count > 0)
        {
            sb.Append(System.Globalization.CultureInfo.InvariantCulture, $" INCLUDE ({QuotedList(command.Definition.IncludeColumns)})");
        }

        if (!string.IsNullOrWhiteSpace(command.Definition.FilterPredicate))
        {
            sb.Append(System.Globalization.CultureInfo.InvariantCulture, $" WHERE {command.Definition.FilterPredicate}");
        }

        if (command.Definition.FillFactor.HasValue)
        {
            sb.Append(System.Globalization.CultureInfo.InvariantCulture, $" WITH (FILLFACTOR = {command.Definition.FillFactor.Value})");
        }

        return sb.ToString();
    }

    internal static string GenerateDropTable(DropTableCommand command)
    {
        var sb = new StringBuilder();
        var fullTableName = Qualified(command.SchemaName, command.TableName);

        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'{Literal(fullTableName)}') AND type = 'U')");
        sb.AppendLine("BEGIN");
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    DROP TABLE {fullTableName}");
        sb.Append("END");

        return sb.ToString();
    }

    internal static string GenerateDropIndex(DropIndexCommand command)
    {
        var sb = new StringBuilder();
        var fullTableName = Qualified(command.SchemaName, command.TableName);

        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'{Literal(command.IndexName)}' AND object_id = OBJECT_ID(N'{Literal(fullTableName)}'))");
        sb.AppendLine("BEGIN");
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    DROP INDEX {Quote(command.IndexName)} ON {fullTableName}");
        sb.Append("END");

        return sb.ToString();
    }

    internal static string GenerateDropSchema(DropSchemaCommand command)
    {
        var sb = new StringBuilder();

        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"IF EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'{Literal(command.Name)}')");
        sb.AppendLine("BEGIN");
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    DROP SCHEMA {Quote(command.Name)}");
        sb.Append("END");

        return sb.ToString();
    }

    // The clause one referential action adds to a foreign key: empty for NO ACTION, which is what SQL Server does
    // when the clause is absent, otherwise the action's own keyword. SQL Server has no RESTRICT, and the NotFound
    // sentinel names no action at all: both are refused, never written as another action (FDW-3610).
    private static IGenericResult<string> ForeignKeyActionClause(DdlForeignKeyDefinition fk, string clause, IForeignKeyAction action)
    {
        if (action.Id == ForeignKeyActions.NotFound.Id || action.Id == ForeignKeyActions.Restrict.Id)
        {
            return GenericResult<string>.Failure(
                DdlResultCodes.ByName("ForeignKeyActionNotSupported"),
                ResultDetails.Create("constraint", fk.Name, "clause", clause, "action", action.Name).With("dialect", Dialect));
        }

        return GenericResult<string>.Success(
            action.Id == ForeignKeyActions.NoAction.Id ? string.Empty : $" {clause} {action.SqlKeyword}");
    }
}
