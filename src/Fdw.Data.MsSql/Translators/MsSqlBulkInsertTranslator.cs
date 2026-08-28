using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Conventions;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql.Logging;
using Fdw.Data.MsSql.Results;
using Fdw.Data.MsSql.Translators;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Translates BulkInsertCommand to SqlBulkCopy operation for maximum performance.
/// </summary>
/// <remarks>
/// <para>
/// Uses SqlBulkCopy for high-performance bulk insert operations.
/// Configures safe defaults:
/// <list type="bullet">
/// <item>CheckConstraints - ensures data integrity</item>
/// <item>FireTriggers - ensures business logic executes</item>
/// <item>KeepIdentity - preserves identity values if provided</item>
/// <item>NO TableLock - uses row-level locking for concurrency</item>
/// </list>
/// </para>
/// <para>
/// ⚠️ Performance vs Safety Trade-off:
/// <list type="bullet">
/// <item>FASTER than batched INSERT for large datasets (10,000+ rows)</item>
/// <item>Uses minimal logging (faster but impacts recovery)</item>
/// <item>May have different locking behavior than standard INSERT</item>
/// </list>
/// </para>
/// <para>
/// Recommended for:
/// <list type="bullet">
/// <item>ETL processes loading large datasets</item>
/// <item>Initial data loads</item>
/// <item>Batch imports where performance is critical</item>
/// </list>
/// </para>
/// <para>
/// Not recommended for:
/// <list type="bullet">
/// <item>Transactional OLTP inserts</item>
/// <item>Small batches (&lt; 1000 rows) - use MsSqlBatchInsertTranslator instead</item>
/// </list>
/// </para>
/// </remarks>
[TypeOption(typeof(MsSqlDataCommandTranslators), "BulkInsert", RestrictToCurrentCompilation = true)]
public sealed class MsSqlBulkInsertTranslator : MsSqlDataCommandTranslatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlBulkInsertTranslator"/> class.
    /// </summary>
    public MsSqlBulkInsertTranslator()
        : base("BulkInsert")
    {
    }

    /// <summary>
    /// Translates a BulkInsertCommand to SqlBulkCopy operation.
    /// </summary>
    /// <remarks>
    /// NOTE: This translator returns a "wrapper" SqlCommand that encapsulates
    /// SqlBulkCopy metadata. The actual bulk copy is executed by MsSqlConnection
    /// when it detects this special command type.
    /// </remarks>
    public override Task<IGenericResult<SqlCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        MsSqlBulkInsertTranslatorLog.Translating(
            NullLogger<MsSqlBulkInsertTranslator>.Instance, container?.Name ?? "<null>");

        try
        {
            if (container == null)
            {
                MsSqlBulkInsertTranslatorLog.ContainerNull(NullLogger<MsSqlBulkInsertTranslator>.Instance);
                return Task.FromResult(
                    GenericResult<SqlCommand>.Failure(MsSqlDataResultCodes.ByName("ContainerNull")));
            }

            if (container.Path is not IDatabasePath dbPath)
            {
                MsSqlBulkInsertTranslatorLog.InvalidContainerPath(
                    NullLogger<MsSqlBulkInsertTranslator>.Instance, container.Name);
                return Task.FromResult(
                    GenericResult<SqlCommand>.Failure(MsSqlDataResultCodes.ByName("InvalidContainerPath")));
            }

            // Get collection data from command
            var dataObj = GetCommandData(command);
            if (dataObj == null)
            {
                MsSqlBulkInsertTranslatorLog.MissingInputData(
                    NullLogger<MsSqlBulkInsertTranslator>.Instance, "BulkInsertCommand", container.Name);
                return Task.FromResult(
                    GenericResult<SqlCommand>.Failure(
                        MsSqlDataResultCodes.ByName("MissingInputData"),
                        ResultDetails.Create("CommandType", "BulkInsertCommand")));
            }

            if (dataObj is not IEnumerable collection)
            {
                MsSqlBulkInsertTranslatorLog.InvalidDataType(
                    NullLogger<MsSqlBulkInsertTranslator>.Instance, container.Name, dataObj.GetType().Name);
                return Task.FromResult(
                    GenericResult<SqlCommand>.Failure(
                        MsSqlDataResultCodes.ByName("InvalidDataType"),
                        ResultDetails.Create("TranslatorName", "MsSqlBulkInsertTranslator", "ActualType", dataObj.GetType().Name)));
            }

            // Build SqlBulkCopy wrapper command
            var sqlCommand = BuildBulkCopyCommand(container, dbPath, collection);

            return Task.FromResult(GenericResult<SqlCommand>.Success(sqlCommand));
        }
        catch (Exception ex)
        {
            MsSqlBulkInsertTranslatorLog.BulkInsertTranslationFailed(
                NullLogger<MsSqlBulkInsertTranslator>.Instance, ex, container?.Name ?? "<null>", ex.Message);
            return Task.FromResult(
                GenericResult<SqlCommand>.Failure(
                    MsSqlDataResultCodes.ByName("BulkInsertTranslationFailed"),
                    ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    /// <summary>
    /// Builds a special SqlCommand that wraps SqlBulkCopy metadata.
    /// MsSqlConnection will detect this and use SqlBulkCopy instead of ExecuteNonQuery.
    /// </summary>
    private static SqlCommand BuildBulkCopyCommand(
        IStorageContainer container,
        IDatabasePath dbPath,
        IEnumerable collection)
    {
        // Exclude system-provided columns (IDENTITY, COMPUTED, DEFAULT-filled like NEWSEQUENTIALID()).
        // IsSystemProvided is the superset (same rule as MsSqlInsertTranslator); mapping a
        // system-provided column here sends DBNull and the copy fails
        // "Column 'X' does not allow DBNull.Value".
        var fields = container.Schema.Fields
            .Where(f => !f.IsIdentity && !f.IsComputed && !f.IsSystemProvided)
            .ToList();

        if (fields.Count == 0)
        {
            MsSqlBulkInsertTranslatorLog.NoInsertableFields(
                NullLogger<MsSqlBulkInsertTranslator>.Instance, container.Name);
            throw new InvalidOperationException($"Container {container.Name} has no insertable fields");
        }

        // Convert collection to DataTable for SqlBulkCopy
        var dataTable = ConvertToDataTable(collection, fields);

        // Get special marker command that MsSqlConnection will recognize
        var command = CreateCommand($"-- BULK INSERT MARKER: {BuildQualifiedTableName(dbPath)}");

        // Store metadata in command for MsSqlConnection to use
        command.CommandType = CommandType.StoredProcedure; // Marker for bulk operation

        // Store DataTable and destination in Parameters collection as metadata
        command.Parameters.Add(new SqlParameter("@__BulkCopy_DataTable", dataTable));
        command.Parameters.Add(new SqlParameter("@__BulkCopy_Destination", BuildSchemaQualifiedTableName(dbPath)));
        command.Parameters.Add(new SqlParameter("@__BulkCopy_ColumnMappings",
            string.Join(",", fields.Select(f => f.Name))));

        MsSqlBulkInsertTranslatorLog.Translated(
            NullLogger<MsSqlBulkInsertTranslator>.Instance, container.Name, dataTable.Rows.Count);

        return command;
    }

    /// <summary>
    /// Converts IEnumerable to DataTable for SqlBulkCopy.
    /// </summary>
    [ConventionOverride(MaxCyclomaticComplexity = 20)]  // DataTable conversion with type inference from field metadata and switch-based type mapping
    private static DataTable ConvertToDataTable(IEnumerable collection, List<IField> fields)
    {
        var dataTable = new DataTable();

        // Add columns
        foreach (var field in fields)
        {
            // Use FieldType.ClrType if available, otherwise infer from TypeName
            var clrType = field.FieldType.ClrType;

            // If ClrType is object, try to infer a better type from TypeName
            if (clrType == typeof(object))
            {
                clrType = field.FieldType.TypeName switch
                {
                    "Int32" => typeof(int),
                    "Int64" => typeof(long),
                    "String" => typeof(string),
                    "DateTime" => typeof(DateTime),
                    "Boolean" => typeof(bool),
                    "Decimal" => typeof(decimal),
                    "Double" => typeof(double),
                    "Guid" => typeof(Guid),
                    _ => typeof(object)
                };
            }

            // Handle nullable types
            var columnType = field.IsNullable && clrType.IsValueType
                ? typeof(Nullable<>).MakeGenericType(clrType)
                : clrType;

            dataTable.Columns.Add(field.Name, Nullable.GetUnderlyingType(columnType) ?? columnType);
        }

        // Add rows
        foreach (var entity in collection)
        {
            var row = dataTable.NewRow();
            FillRow(row, entity, fields);
            dataTable.Rows.Add(row);
        }

        return dataTable;
    }

    private static void FillRow(DataRow row, object entity, List<IField> fields)
    {
        if (entity is IDictionary<string, object?> dict)
        {
            foreach (var field in fields)
                row[field.Name] = dict.TryGetValue(field.Name, out var v) && v is not null ? v : DBNull.Value;
            return;
        }

        var entityType = entity.GetType();
        foreach (var field in fields)
        {
            var value = entityType.GetProperty(field.Name)?.GetValue(entity);
            row[field.Name] = value ?? DBNull.Value;
        }
    }
}
