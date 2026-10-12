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
using Fdw.Data.PostgreSql.Results;
using Fdw.Data.PostgreSql.Translators;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Npgsql;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Translates InsertCommand with collection data to batched multi-row PostgreSQL INSERT statements.
/// </summary>
/// <remarks>
/// <para>
/// ACID-compliant batch insert using PostgreSQL multi-row VALUES syntax:
/// <code>
/// INSERT INTO "schema"."table" ("col1", "col2", "col3")
/// VALUES
///   (@p0, @p1, @p2),
///   (@p3, @p4, @p5),
///   (@p6, @p7, @p8)
/// </code>
/// </para>
/// <para>
/// PostgreSQL does not have the same 1000-row or 2100-parameter limits as SQL Server,
/// but a conservative batch size is maintained for memory efficiency and to avoid
/// excessively large SQL statements.
/// </para>
/// </remarks>
[TypeOption(typeof(PostgreSqlDataCommandTranslators), "BatchInsert", RestrictToCurrentCompilation = true)]
public sealed class PostgreSqlBatchInsertTranslator : PostgreSqlDataCommandTranslatorBase
{
    private const int MaxParametersPerStatement = 32767; // PostgreSQL limit (Int16.MaxValue)

    /// <summary>
    /// The most rows one INSERT statement carries, before the parameter limit lowers it further. A
    /// deployment sets this before the host starts.
    /// </summary>
    public static int MaxRowsPerBatch { get; set; } = 1000;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlBatchInsertTranslator"/> class.
    /// </summary>
    public PostgreSqlBatchInsertTranslator()
        : base("BatchInsert")
    {
    }

    /// <summary>
    /// Translates an InsertCommand with collection data to batched PostgreSQL INSERT statements.
    /// </summary>
    public override Task<IGenericResult<NpgsqlCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (container == null)
            {
                return Task.FromResult(
                    GenericResult<NpgsqlCommand>.Failure(PostgreSqlDataResultCodes.ByName("ContainerNull")));
            }

            if (container.Path is not IDatabasePath dbPath)
            {
                return Task.FromResult(
                    GenericResult<NpgsqlCommand>.Failure(PostgreSqlDataResultCodes.ByName("InvalidContainerPath")));
            }

            var dataObj = GetCommandData(command);
            if (dataObj == null)
            {
                return Task.FromResult(
                    GenericResult<NpgsqlCommand>.Failure(
                        PostgreSqlDataResultCodes.ByName("MissingInputData"),
                        ResultDetails.Create("CommandType", "InsertCommand")));
            }

            if (dataObj is not IEnumerable collection)
            {
                return Task.FromResult(
                    GenericResult<NpgsqlCommand>.Failure(
                        PostgreSqlDataResultCodes.ByName("InvalidDataType"),
                        ResultDetails.Create("TranslatorName", "PostgreSqlBatchInsertTranslator", "ActualType", dataObj.GetType().Name)));
            }

            var npgsqlCommand = BuildBatchedInsertStatements(container, dbPath, collection);

            return Task.FromResult(GenericResult<NpgsqlCommand>.Success(npgsqlCommand));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                GenericResult<NpgsqlCommand>.Failure(
                    PostgreSqlDataResultCodes.ByName("BatchInsertTranslationFailed"),
                    ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    /// <summary>
    /// The columns an insert writes: all but the ones the database provides (identity and computed).
    /// </summary>
    /// <param name="container">The container the rows are inserted into.</param>
    /// <returns>The insertable fields, in schema order.</returns>
    internal static List<IField> InsertableFields(IStorageContainer container)
        => container.Schema.Fields
            .Where(f => !f.IsIdentity && !f.IsComputed)
            .ToList();

    /// <summary>
    /// The most rows of <paramref name="container"/> that one command can insert, which is as many as keep it within the
    /// parameter limit (one parameter per value).
    /// </summary>
    /// <remarks>
    /// The connection chooses between the serial route and the COPY with this, as well as with its configured
    /// <c>MaxSerialInsert</c>: a bulk insert of more rows than a command can carry goes to the COPY, which has no such limit. A
    /// container with no insertable column has no command to size, so its limit is unbounded here and the translator refuses it
    /// with its own coded failure.
    /// </remarks>
    /// <param name="container">The container the rows are inserted into.</param>
    /// <returns>The row limit of one command.</returns>
    public static int RowsPerRequest(IStorageContainer container)
    {
        var columns = InsertableFields(container).Count;
        return columns == 0 ? int.MaxValue : MaxParametersPerStatement / columns;
    }

    /// <summary>
    /// Builds batched multi-row INSERT statements for PostgreSQL.
    /// </summary>
    internal static NpgsqlCommand BuildBatchedInsertStatements(
        IStorageContainer container,
        IDatabasePath dbPath,
        IEnumerable collection)
    {
        var dialect = dbPath.Dialect;
        var p = dialect.ParameterPrefix;

        var fields = InsertableFields(container);

        if (fields.Count == 0)
        {
            throw new InvalidOperationException($"Container {container.Name} has no insertable fields");
        }

        var fieldNames = fields.Select(f => f.Name).ToList();
        var columnList = string.Join(", ", fieldNames.Select(f => dialect.QuoteIdentifier(f)));

        // Calculate safe batch size
        var columnsPerRow = fieldNames.Count;
        var maxRowsPerBatch = Math.Min(
            MaxRowsPerBatch,
            MaxParametersPerStatement / columnsPerRow);

        if (maxRowsPerBatch < 1)
        {
            maxRowsPerBatch = 1;
        }

        // Convert collection to list for batching
        var entities = collection.Cast<object>().ToList();

        if (entities.Count == 0)
        {
            throw new InvalidOperationException("Cannot insert empty collection");
        }

        // One command carries at most MaxParametersPerStatement parameters however many statements it holds, so rows beyond
        // that cannot be sent as this command. They are refused here with the reason; the connection routes a bulk insert of more
        // rows than this to its COPY before it translates (RowsPerRequest).
        if (entities.Count * columnsPerRow > MaxParametersPerStatement)
        {
            throw new InvalidOperationException(
                $"{entities.Count} rows of {columnsPerRow} columns need {entities.Count * columnsPerRow} parameters; "
                + $"one PostgreSQL command carries at most {MaxParametersPerStatement}, which is {RowsPerRequest(container)} rows of this container");
        }

        // Build multi-statement SQL for all batches
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

                    // The row is read the one way every bulk route reads it (RowValues): a dictionary row, which is what a
                    // pipeline load carries, binds its values as an entity row does.
                    RowValues.TryRead(entity, fieldName, out var value, out _);
                    allParameters.Add(new NpgsqlParameter(paramName, value ?? System.DBNull.Value));
                }

                valuesClauses.Add($"({string.Join(", ", paramNames)})");
            }

            var batchSql = $"INSERT INTO {BuildQualifiedTableName(dbPath)} ({columnList}) VALUES {string.Join(", ", valuesClauses)};";
            sqlBatches.Add(batchSql);
        }

        // Combine all batches
        var fullSql = string.Join("\n", sqlBatches);

        var command = CreateCommand(fullSql);
        foreach (var param in allParameters)
        {
            command.Parameters.Add(param);
        }

        return command;
    }
}
