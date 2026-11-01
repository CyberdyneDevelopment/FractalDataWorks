using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Rows;
using Fdw.Data.Sqlite.Logging;
using Fdw.Data.Sqlite.Results;
using Fdw.Data.Sqlite.Translators;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Sql;
using Microsoft.Data.Sqlite;

namespace Fdw.Services.Connections.Sqlite;

/// <summary>
/// Translates an <c>InsertCommand</c>, one row or many, into batched multi-row INSERT statements for SQLite.
/// </summary>
/// <remarks>
/// SQLite has no bulk path, so every insert runs serially, however many rows it carries; each statement holds as many rows as
/// SQLite's parameter limit allows.
/// </remarks>
[TypeOption(typeof(SqliteDataCommandTranslators), "Insert", RestrictToCurrentCompilation = true)]
public sealed class SqliteInsertTranslator : SqliteDataCommandTranslatorBase
{
    private const int MaxParametersPerStatement = 999;

    /// <summary>Initializes a new instance of the <see cref="SqliteInsertTranslator"/> class.</summary>
    public SqliteInsertTranslator()
        : base("Insert")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<SqliteCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (container == null)
                return Task.FromResult(GenericResult<SqliteCommand>.Failure(SqliteDataResultCodes.ByName("ContainerNull")));

            if (container.Path is not IDatabasePath dbPath)
                return Task.FromResult(GenericResult<SqliteCommand>.Failure(SqliteDataResultCodes.ByName("InvalidContainerPath")));

            var dataObj = GetCommandData(command);
            if (dataObj == null)
                return Task.FromResult(
                    GenericResult<SqliteCommand>.Failure(
                        SqliteDataResultCodes.ByName("MissingInputData"),
                        ResultDetails.Create("CommandType", "InsertCommand")));

            if (dataObj is not IEnumerable rows)
                return Task.FromResult(
                    GenericResult<SqliteCommand>.Failure(
                        SqliteDataResultCodes.ByName("InvalidDataType"),
                        ResultDetails.Create("ActualType", dataObj.GetType().Name)));

            return Task.FromResult(GenericResult<SqliteCommand>.Success(BuildBatchedInsertStatements(container, dbPath, rows, ReturnedColumnsOf(command, container))));
        }
        catch (Exception ex)
        {
            SqliteConnectionLog.TranslationFailed(TranslatorLogger, ex, "Insert", ex.Message);
            return Task.FromResult(
                GenericResult<SqliteCommand>.Failure(
                    SqliteDataResultCodes.ByName("InsertTranslationFailed"),
                    ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    private static SqliteCommand BuildBatchedInsertStatements(
        IStorageContainer container,
        IDatabasePath dbPath,
        IEnumerable collection,
        IReadOnlyList<string> returned)
    {
        var dialect = dbPath.Dialect;

        var insertable = container.Schema.Fields
            .Where(f => !f.IsIdentity && !f.IsComputed && !f.IsSystemProvided)
            .ToList();
        if (insertable.Count == 0)
            throw new InvalidOperationException($"Container {container.Name} has no insertable fields");

        var entities = collection.Cast<object>().ToList();
        if (entities.Count == 0)
            throw new InvalidOperationException("Cannot insert empty collection");

        // The columns the rows supply: a column no row supplies is left out so its database default applies.
        var fields = InsertColumns.Supplied(insertable, entities);
        if (fields.Count == 0)
            throw new InvalidOperationException($"The rows have no properties matching insertable fields for container {container.Name}");

        var fieldNames = fields.Select(f => f.Name).ToList();
        var columnList = string.Join(", ", fieldNames.Select(f => dialect.QuoteIdentifier(f)));
        var p = dialect.ParameterPrefix;
        var returning = returned.Count == 0
            ? string.Empty
            : $" RETURNING {string.Join(", ", returned.Select(c => dialect.QuoteIdentifier(c)))}";

        var columnsPerRow = fieldNames.Count;
        var maxRowsPerBatch = Math.Max(1, MaxParametersPerStatement / columnsPerRow);

        var sqlBatches = new List<string>();
        var allParameters = new List<SqliteParameter>();
        var parameterIndex = 0;

        for (var i = 0; i < entities.Count; i += maxRowsPerBatch)
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
                    allParameters.Add(new SqliteParameter(paramName, value ?? DBNull.Value));
                }

                valuesClauses.Add($"({string.Join(", ", paramNames)})");
            }

            sqlBatches.Add($"INSERT INTO {BuildQualifiedTableName(dbPath)} ({columnList}) VALUES {string.Join(", ", valuesClauses)}{returning};");
        }

        var fullSql = string.Join("\n", sqlBatches) + (returned.Count == 0 ? "\nSELECT changes();" : string.Empty);
        var command = CreateCommand(fullSql);
        foreach (var param in allParameters)
            command.Parameters.Add(param);

        return command;
    }
}
