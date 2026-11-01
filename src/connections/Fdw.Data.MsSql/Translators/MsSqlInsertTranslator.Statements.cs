using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Rows;
using Fdw.Data.MsSql.Logging;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.MsSql;

public sealed partial class MsSqlInsertTranslator
{
    private const int MaxRowsPerInsert = 1000; // SQL Server hard limit
    private const int MaxParametersPerStatement = 2100; // SQL Server hard limit

    internal static List<IField> InsertableFields(IStorageContainer container)
        => container.Schema.Fields
            .Where(f => !f.IsIdentity && !f.IsComputed && !f.IsSystemProvided)
            .ToList();

    /// <summary>The most rows of this container one SQL Server request carries.</summary>
    /// <param name="container">The container the rows go into.</param>
    /// <returns>The number of rows that fit within the request's parameter limit.</returns>
    public static int RowsPerRequest(IStorageContainer container)
    {
        var columns = InsertableFields(container).Count;
        return columns == 0 ? int.MaxValue : MaxParametersPerStatement / columns;
    }

    // One request carries at most MaxParametersPerStatement parameters however many statements it holds, so rows beyond that
    // cannot be sent as one command. They are refused here with the reason; the connection routes an insert of more rows than
    // a request carries to its bulk copy before it translates (RowsPerRequest), so only an insert with no such route reaches this.
    private static void EnsureWithinParameterLimit(IStorageContainer container, int rowCount, int columnCount)
    {
        var parameterCount = rowCount * columnCount;
        if (parameterCount <= MaxParametersPerStatement)
            return;

        MsSqlInsertTranslatorLog.TooManyParameters(
            NullLogger<MsSqlInsertTranslator>.Instance,
            container.Name, rowCount, columnCount, parameterCount, MaxParametersPerStatement);
        throw new InvalidOperationException(
            $"{rowCount} rows of {columnCount} columns need {parameterCount} parameters; one SQL Server request carries at most "
            + $"{MaxParametersPerStatement}, which is {RowsPerRequest(container)} rows of this container");
    }

    // The type that gives a null its SQL type (FDW-918): the entity property's, or the column's for a dictionary row, which
    // declares none. A column the row lacks is a null with no type to give it, and a value needs none.
    private static Type? NullBindingType(IField field, bool found, object? value, Type? declaredType)
        => declaredType ?? (found && value is null ? field.FieldType.ClrType : null);

    internal static SqlCommand BuildBatchedInsertStatements(
        IStorageContainer container,
        IDatabasePath dbPath,
        IEnumerable collection,
        IReadOnlyList<string> returned)
    {
        var dialect = dbPath.Dialect;

        var insertable = InsertableFields(container);
        if (insertable.Count == 0)
        {
            MsSqlInsertTranslatorLog.NoInsertableFields(
                NullLogger<MsSqlInsertTranslator>.Instance, container.Name);
            throw new InvalidOperationException($"Container {container.Name} has no insertable fields");
        }

        var entities = collection.Cast<object>().ToList();
        if (entities.Count == 0)
        {
            MsSqlInsertTranslatorLog.EmptyCollection(
                NullLogger<MsSqlInsertTranslator>.Instance, container.Name);
            throw new InvalidOperationException("Cannot insert empty collection");
        }

        // The columns the rows supply: a column no row supplies is left out so its database default applies.
        var fields = InsertColumns.Supplied(insertable, entities);
        if (fields.Count == 0)
        {
            MsSqlInsertTranslatorLog.NoMatchingProperties(
                NullLogger<MsSqlInsertTranslator>.Instance, container.Name);
            throw new InvalidOperationException(
                $"The rows have no properties matching insertable fields for container {container.Name}");
        }

        var fieldNames = fields.Select(f => f.Name).ToList();
        var columnList = string.Join(", ", fieldNames.Select(f => dialect.QuoteIdentifier(f)));
        // Columns to return come back as rows of each INSERT's OUTPUT clause, so the statement count that would be a row
        // in the same result is left out.
        var output = returned.Count == 0
            ? string.Empty
            : $" OUTPUT {string.Join(", ", returned.Select(c => $"inserted.{dialect.QuoteIdentifier(c)}"))}";
        var p = dialect.ParameterPrefix;

        // Calculate safe batch size
        var columnsPerRow = fieldNames.Count;
        var maxRowsPerBatch = Math.Min(
            MaxRowsPerInsert,
            MaxParametersPerStatement / columnsPerRow);
        if (maxRowsPerBatch < 1)
        {
            maxRowsPerBatch = 1; // At least 1 row
        }

        EnsureWithinParameterLimit(container, entities.Count, columnsPerRow);

        // Build multi-statement SQL for all batches
        var sqlBatches = new List<string>();
        var bindings = new List<Action<SqlCommand>>();
        var parameterIndex = 0;

        for (int i = 0; i < entities.Count; i += maxRowsPerBatch)
        {
            var batch = entities.Skip(i).Take(maxRowsPerBatch).ToList();
            var valuesClauses = new List<string>();

            foreach (var entity in batch)
            {
                var paramNames = new List<string>();
                foreach (var field in fields)
                {
                    var paramName = $"p{parameterIndex++}";
                    paramNames.Add($"{p}{paramName}");

                    // The row is read the one way every route reads it (RowValues), so a dictionary row, which is what a
                    // pipeline load carries, binds its values as an entity row does. Bound through the shared binder, so a
                    // row binds the same however many rows travel with it: byte[] as binary, a collection as JSON, a null typed
                    // by its declared type (FDW-918).
                    var found = RowValues.TryRead(entity, field.Name, out var value, out var declaredType);
                    var bindingType = NullBindingType(field, found, value, declaredType);
                    bindings.Add(bindingType is null
                        ? target => AddParameter(target, paramName, value)
                        : target => AddParameter(target, paramName, value, bindingType));
                }

                valuesClauses.Add($"({string.Join(", ", paramNames)})");
            }

            var batchSql = $"INSERT INTO {BuildQualifiedTableName(dbPath)} ({columnList}){output} VALUES {string.Join(", ", valuesClauses)};";
            sqlBatches.Add(batchSql);
        }

        // Combine all batches + row count
        var fullSql = string.Join("\n", sqlBatches) + (returned.Count == 0 ? "\nSELECT @@ROWCOUNT;" : string.Empty);

        // Get command with all parameters
        var command = CreateCommand(fullSql);
        foreach (var bind in bindings)
        {
            bind(command);
        }

        MsSqlInsertTranslatorLog.Translated(
            NullLogger<MsSqlInsertTranslator>.Instance, container.Name, sqlBatches.Count, entities.Count);
        return command;
    }
}
