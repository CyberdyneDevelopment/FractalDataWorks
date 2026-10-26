using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Rows;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Sql;
using Npgsql;

namespace Fdw.Data.PostgreSql;

public sealed partial class PostgreSqlInsertTranslator
{
    private const int MaxParametersPerStatement = 32767; // PostgreSQL limit (Int16.MaxValue)

    /// <summary>Gets or sets the most rows one INSERT statement carries.</summary>
    public static int MaxRowsPerBatch { get; set; } = 1000;

    internal static List<IField> InsertableFields(IStorageContainer container)
        => container.Schema.Fields
            .Where(f => !f.IsIdentity && !f.IsComputed)
            .ToList();

    /// <summary>The most rows of this container one PostgreSQL command carries.</summary>
    /// <param name="container">The container the rows go into.</param>
    /// <returns>The number of rows that fit within the command's parameter limit.</returns>
    public static int RowsPerRequest(IStorageContainer container)
    {
        var columns = InsertableFields(container).Count;
        return columns == 0 ? int.MaxValue : MaxParametersPerStatement / columns;
    }

    internal static NpgsqlCommand BuildBatchedInsertStatements(
        IStorageContainer container,
        IDatabasePath dbPath,
        IEnumerable collection,
        IReadOnlyList<string> returned)
    {
        var dialect = dbPath.Dialect;
        var p = dialect.ParameterPrefix;

        var insertable = InsertableFields(container);
        if (insertable.Count == 0)
        {
            throw new InvalidOperationException($"Container {container.Name} has no insertable fields");
        }

        var entities = collection.Cast<object>().ToList();
        if (entities.Count == 0)
        {
            throw new InvalidOperationException("Cannot insert empty collection");
        }

        // The columns the rows supply: a column no row supplies is left out so its database default applies.
        var fields = InsertColumns.Supplied(insertable, entities);
        if (fields.Count == 0)
        {
            throw new InvalidOperationException(
                $"The rows have no properties matching insertable fields for container {container.Name}");
        }

        var fieldNames = fields.Select(f => f.Name).ToList();
        var columnList = string.Join(", ", fieldNames.Select(f => dialect.QuoteIdentifier(f)));
        var returning = returned.Count == 0
            ? string.Empty
            : $" RETURNING {string.Join(", ", returned.Select(c => dialect.QuoteIdentifier(c)))}";

        var columnsPerRow = fieldNames.Count;
        var maxRowsPerBatch = Math.Min(
            MaxRowsPerBatch,
            MaxParametersPerStatement / columnsPerRow);
        if (maxRowsPerBatch < 1)
        {
            maxRowsPerBatch = 1;
        }

        // One command carries at most MaxParametersPerStatement parameters however many statements it holds, so rows beyond that
        // cannot be sent as one command. The connection routes an insert of more rows than a command carries to its COPY before it
        // translates (RowsPerRequest), so only an insert with no such route reaches this.
        if (entities.Count * columnsPerRow > MaxParametersPerStatement)
        {
            throw new InvalidOperationException(
                $"{entities.Count} rows of {columnsPerRow} columns need {entities.Count * columnsPerRow} parameters; "
                + $"one PostgreSQL command carries at most {MaxParametersPerStatement}, which is {RowsPerRequest(container)} rows of this container");
        }

        var sqlBatches = new List<string>();
        var allParameters = new List<NpgsqlParameter>();
        var parameterIndex = 0;

        for (int i = 0; i < entities.Count; i += maxRowsPerBatch)
        {
            var batch = entities.Skip(i).Take(maxRowsPerBatch).ToList();
            var valuesClauses = new List<string>();

            foreach (var entity in batch)
            {
                var paramNames = new List<string>();
                foreach (var fieldName in fieldNames)
                {
                    var paramName = $"{p}p{parameterIndex++}";
                    paramNames.Add(paramName);

                    // The row is read the one way every route reads it (RowValues), so a dictionary row binds as an entity row does.
                    RowValues.TryRead(entity, fieldName, out var value, out _);
                    allParameters.Add(new NpgsqlParameter(paramName, value ?? System.DBNull.Value));
                }

                valuesClauses.Add($"({string.Join(", ", paramNames)})");
            }

            var batchSql = $"INSERT INTO {BuildQualifiedTableName(dbPath)} ({columnList}) VALUES {string.Join(", ", valuesClauses)}{returning};";
            sqlBatches.Add(batchSql);
        }

        var fullSql = string.Join("\n", sqlBatches);
        var command = CreateCommand(fullSql);
        foreach (var param in allParameters)
        {
            command.Parameters.Add(param);
        }

        return command;
    }
}
