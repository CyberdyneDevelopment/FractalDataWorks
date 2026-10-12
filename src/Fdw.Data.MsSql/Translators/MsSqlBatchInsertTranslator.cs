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
using Fdw.Data.MsSql.Logging;
using Fdw.Data.MsSql.Results;
using Fdw.Data.MsSql.Translators;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Translates InsertCommand&lt;IEnumerable&lt;T&gt;&gt; to batched multi-row T-SQL INSERT statements.
/// </summary>
/// <remarks>
/// <para>
/// ACID-compliant batch insert using SQL Server multi-row VALUES syntax:
/// <code>
/// INSERT INTO table (col1, col2, col3)
/// VALUES
///   (@p0, @p1, @p2),
///   (@p3, @p4, @p5),
///   (@p6, @p7, @p8)
/// </code>
/// </para>
/// <para>
/// Limitations:
/// <list type="bullet">
/// <item>SQL Server limit: 1000 rows per INSERT statement</item>
/// <item>SQL Server limit: 2100 parameters per statement</item>
/// </list>
/// </para>
/// <para>
/// Batching strategy:
/// <list type="bullet">
/// <item>Calculate max rows per batch based on column count (stay under 2100 params)</item>
/// <item>Default: 500 rows per batch (safe for tables with ~4 columns)</item>
/// <item>Executes multiple INSERT statements if needed (within single transaction)</item>
/// </list>
/// </para>
/// <para>
/// ✅ Fully ACID compliant - triggers fire, constraints checked, row-level locking
/// </para>
/// </remarks>
[TypeOption(typeof(MsSqlDataCommandTranslators), "BatchInsert", RestrictToCurrentCompilation = true)]
public sealed class MsSqlBatchInsertTranslator : MsSqlDataCommandTranslatorBase
{
    private const int MaxRowsPerInsert = 1000; // SQL Server hard limit
    private const int MaxParametersPerStatement = 2100; // SQL Server hard limit

    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlBatchInsertTranslator"/> class.
    /// </summary>
    public MsSqlBatchInsertTranslator()
        : base("BatchInsert")
    {
    }

    /// <summary>
    /// Translates an InsertCommand with collection data to batched T-SQL INSERT statements.
    /// </summary>
    public override Task<IGenericResult<SqlCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        MsSqlBatchInsertTranslatorLog.Translating(
            NullLogger<MsSqlBatchInsertTranslator>.Instance, container?.Name ?? "<null>");

        try
        {
            if (container == null)
            {
                MsSqlBatchInsertTranslatorLog.ContainerNull(NullLogger<MsSqlBatchInsertTranslator>.Instance);
                return Task.FromResult(
                    GenericResult<SqlCommand>.Failure(MsSqlDataResultCodes.ByName("ContainerNull")));
            }

            if (container.Path is not IDatabasePath dbPath)
            {
                MsSqlBatchInsertTranslatorLog.InvalidContainerPath(
                    NullLogger<MsSqlBatchInsertTranslator>.Instance, container.Name);
                return Task.FromResult(
                    GenericResult<SqlCommand>.Failure(MsSqlDataResultCodes.ByName("InvalidContainerPath")));
            }

            // Get collection data from command
            var dataObj = GetCommandData(command);
            if (dataObj == null)
            {
                MsSqlBatchInsertTranslatorLog.MissingInputData(
                    NullLogger<MsSqlBatchInsertTranslator>.Instance, "InsertCommand", container.Name);
                return Task.FromResult(
                    GenericResult<SqlCommand>.Failure(
                        MsSqlDataResultCodes.ByName("MissingInputData"),
                        ResultDetails.Create("CommandType", "InsertCommand")));
            }

            // Ensure data is a collection
            if (dataObj is not IEnumerable collection)
            {
                MsSqlBatchInsertTranslatorLog.InvalidDataType(
                    NullLogger<MsSqlBatchInsertTranslator>.Instance, container.Name, dataObj.GetType().Name);
                return Task.FromResult(
                    GenericResult<SqlCommand>.Failure(
                        MsSqlDataResultCodes.ByName("InvalidDataType"),
                        ResultDetails.Create("TranslatorName", "MsSqlBatchInsertTranslator", "ActualType", dataObj.GetType().Name)));
            }

            // Build batched INSERT statements
            var sqlCommand = BuildBatchedInsertStatements(container, dbPath, collection);

            return Task.FromResult(GenericResult<SqlCommand>.Success(sqlCommand));
        }
        catch (Exception ex)
        {
            MsSqlBatchInsertTranslatorLog.BatchInsertTranslationFailed(
                NullLogger<MsSqlBatchInsertTranslator>.Instance, ex, container?.Name ?? "<null>", ex.Message);
            return Task.FromResult(
                GenericResult<SqlCommand>.Failure(
                    MsSqlDataResultCodes.ByName("BatchInsertTranslationFailed"),
                    ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    /// <summary>
    /// The columns an insert writes: all but the ones the database provides (IDENTITY, COMPUTED, DEFAULT-filled like
    /// NEWSEQUENTIALID()).
    /// </summary>
    /// <remarks>
    /// IsSystemProvided is the superset (same rule as MsSqlInsertTranslator); mapping a system-provided column here sends DBNull
    /// and the copy fails "Column 'X' does not allow DBNull.Value".
    /// </remarks>
    /// <param name="container">The container the rows are inserted into.</param>
    /// <returns>The insertable fields, in schema order.</returns>
    internal static List<IField> InsertableFields(IStorageContainer container)
        => container.Schema.Fields
            .Where(f => !f.IsIdentity && !f.IsComputed && !f.IsSystemProvided)
            .ToList();

    /// <summary>
    /// The most rows of <paramref name="container"/> that one SQL Server request can insert, which is as many as keep the
    /// request within the 2100 parameters it accepts (one parameter per value).
    /// </summary>
    /// <remarks>
    /// The connection chooses between the serial route and the bulk copy with this, as well as with its configured
    /// <c>MaxSerialInsert</c>: a bulk insert of more rows than a request can carry goes to the bulk copy, which has no such
    /// limit. A container with no insertable column has no request to size, so its limit is unbounded here and the translator
    /// refuses it with its own coded failure.
    /// </remarks>
    /// <param name="container">The container the rows are inserted into.</param>
    /// <returns>The row limit of one request.</returns>
    public static int RowsPerRequest(IStorageContainer container)
    {
        var columns = InsertableFields(container).Count;
        return columns == 0 ? int.MaxValue : MaxParametersPerStatement / columns;
    }

    // One request carries at most MaxParametersPerStatement parameters however many statements it holds, so rows beyond that
    // cannot be sent as one command. They are refused here with the reason; the connection routes a bulk insert of more rows than
    // a request carries to its bulk copy before it translates (RowsPerRequest), so only an insert with no such route reaches this.
    private static void EnsureWithinParameterLimit(IStorageContainer container, int rowCount, int columnCount)
    {
        var parameterCount = rowCount * columnCount;
        if (parameterCount <= MaxParametersPerStatement)
            return;

        MsSqlBatchInsertTranslatorLog.TooManyParameters(
            NullLogger<MsSqlBatchInsertTranslator>.Instance,
            container.Name, rowCount, columnCount, parameterCount, MaxParametersPerStatement);
        throw new InvalidOperationException(
            $"{rowCount} rows of {columnCount} columns need {parameterCount} parameters; one SQL Server request carries at most "
            + $"{MaxParametersPerStatement}, which is {RowsPerRequest(container)} rows of this container");
    }

    // The type that gives a null its SQL type (FDW-918): the entity property's, or the column's for a dictionary row, which
    // declares none. A column the row lacks is a null with no type to give it, and a value needs none.
    private static Type? NullBindingType(IField field, bool found, object? value, Type? declaredType)
        => declaredType ?? (found && value is null ? field.FieldType.ClrType : null);

    /// <summary>
    /// Builds batched multi-row INSERT statements.
    /// </summary>
    internal static SqlCommand BuildBatchedInsertStatements(
        IStorageContainer container,
        IDatabasePath dbPath,
        IEnumerable collection)
    {
        var dialect = dbPath.Dialect;

        var fields = InsertableFields(container);

        if (fields.Count == 0)
        {
            MsSqlBatchInsertTranslatorLog.NoInsertableFields(
                NullLogger<MsSqlBatchInsertTranslator>.Instance, container.Name);
            throw new InvalidOperationException($"Container {container.Name} has no insertable fields");
        }

        var fieldNames = fields.Select(f => f.Name).ToList();
        var columnList = string.Join(", ", fieldNames.Select(f => dialect.QuoteIdentifier(f)));
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

        // Convert collection to list for batching
        var entities = collection.Cast<object>().ToList();

        if (entities.Count == 0)
        {
            MsSqlBatchInsertTranslatorLog.EmptyCollection(
                NullLogger<MsSqlBatchInsertTranslator>.Instance, container.Name);
            throw new InvalidOperationException("Cannot insert empty collection");
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

                    // The row is read the one way every bulk route reads it (RowValues), so a dictionary row, which is what a
                    // pipeline load carries, binds its values as an entity row does. Bound through the shared binder, so a
                    // batch row binds exactly as a single insert does: byte[] as binary, a collection as JSON, a null typed
                    // by its declared type (FDW-918).
                    var found = RowValues.TryRead(entity, field.Name, out var value, out var declaredType);
                    var bindingType = NullBindingType(field, found, value, declaredType);
                    bindings.Add(bindingType is null
                        ? target => AddParameter(target, paramName, value)
                        : target => AddParameter(target, paramName, value, bindingType));
                }

                valuesClauses.Add($"({string.Join(", ", paramNames)})");
            }

            var batchSql = $"INSERT INTO {BuildQualifiedTableName(dbPath)} ({columnList}) VALUES {string.Join(", ", valuesClauses)};";
            sqlBatches.Add(batchSql);
        }

        // Combine all batches + row count
        var fullSql = string.Join("\n", sqlBatches) + "\nSELECT @@ROWCOUNT;";

        // Get command with all parameters
        var command = CreateCommand(fullSql);
        foreach (var bind in bindings)
        {
            bind(command);
        }

        MsSqlBatchInsertTranslatorLog.Translated(
            NullLogger<MsSqlBatchInsertTranslator>.Instance, container.Name, sqlBatches.Count, entities.Count);

        return command;
    }
}
