using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.Sqlite.Logging;
using Fdw.Data.Sqlite.Results;
using Fdw.Data.Sqlite.Translators;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Data.Sqlite;

namespace Fdw.Services.Connections.Sqlite;

/// <summary>
/// Translates InsertCommand&lt;IEnumerable&lt;T&gt;&gt; to batched multi-row SQLite INSERT statements.
/// Uses multi-row VALUES syntax: INSERT INTO t (c1, c2) VALUES (@p0, @p1), (@p2, @p3), ...
/// </summary>
/// <remarks>
/// SQLite has a limit of 999 parameters per statement (SQLITE_MAX_VARIABLE_NUMBER).
/// Batches are sized to stay within this limit based on column count.
/// </remarks>
[TypeOption(typeof(SqliteDataCommandTranslators), "BatchInsert", RestrictToCurrentCompilation = true)]
public sealed class SqliteBatchInsertTranslator : SqliteDataCommandTranslatorBase
{
    private const int MaxParametersPerStatement = 999;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteBatchInsertTranslator"/> class.
    /// </summary>
    public SqliteBatchInsertTranslator()
        : base("BatchInsert")
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

            if (dataObj is not IEnumerable collection)
                return Task.FromResult(
                    GenericResult<SqliteCommand>.Failure(
                        SqliteDataResultCodes.ByName("InvalidDataType"),
                        ResultDetails.Create("ActualType", dataObj.GetType().Name)));

            return Task.FromResult(GenericResult<SqliteCommand>.Success(BuildBatchedInsertStatements(container, dbPath, collection)));
        }
        catch (Exception ex)
        {
            SqliteConnectionLog.TranslationFailed(TranslatorLogger, ex, "BatchInsert", ex.Message);
            return Task.FromResult(
                GenericResult<SqliteCommand>.Failure(
                    SqliteDataResultCodes.ByName("BatchInsertTranslationFailed"),
                    ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

#pragma warning disable MA0051
    private static SqliteCommand BuildBatchedInsertStatements(
        IStorageContainer container,
        IDatabasePath dbPath,
        IEnumerable collection)
#pragma warning restore MA0051
    {
        var dialect = dbPath.Dialect;

        var fields = container.Schema.Fields
            .Where(f => !f.IsIdentity && !f.IsComputed)
            .ToList();

        if (fields.Count == 0)
            throw new InvalidOperationException($"Container {container.Name} has no insertable fields");

        var fieldNames = fields.Select(f => f.Name).ToList();
        var columnList = string.Join(", ", fieldNames.Select(f => dialect.QuoteIdentifier(f)));
        var p = dialect.ParameterPrefix;

        var columnsPerRow = fieldNames.Count;
        var maxRowsPerBatch = Math.Max(1, MaxParametersPerStatement / columnsPerRow);

        var entities = collection.Cast<object>().ToList();
        if (entities.Count == 0)
            throw new InvalidOperationException("Cannot insert empty collection");

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
                    var value = entity.GetType().GetProperty(fieldName)?.GetValue(entity);
                    allParameters.Add(new SqliteParameter(paramName, value ?? DBNull.Value));
                }
                valuesClauses.Add($"({string.Join(", ", paramNames)})");
            }

            sqlBatches.Add($"INSERT INTO {BuildQualifiedTableName(dbPath)} ({columnList}) VALUES {string.Join(", ", valuesClauses)};");
        }

        var fullSql = string.Join("\n", sqlBatches) + "\nSELECT changes();";
        var command = CreateCommand(fullSql);
        foreach (var param in allParameters)
            command.Parameters.Add(param);

        return command;
    }
}
