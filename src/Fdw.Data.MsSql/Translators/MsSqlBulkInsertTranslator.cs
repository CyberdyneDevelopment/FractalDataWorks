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
/// Translates BulkInsertCommand to batched multi-row INSERT statements, and plans the SqlBulkCopy the connection runs when the
/// command is rerouted to bulk (FDW-1152).
/// </summary>
/// <remarks>
/// <para>
/// A bulk insert runs one of two ways, chosen by the connection before it translates: up to the connection's configured
/// <c>MaxSerialInsert</c> rows it runs serially, and this translator gives it the same batched INSERT statements a batch insert
/// gets, so it runs identically in and out of a unit of work; above that the command answers
/// <see cref="IDatabaseConnectionCommand.ReroutesToBulk"/> and the connection takes the bulk route with <see cref="Plan"/>,
/// which builds the rows and the destination and nothing else. No SQL is faked: the translator never returns a command that is
/// not SQL.
/// </para>
/// <para>
/// The bulk copy (SqlBulkCopy) is configured by the connection with safe defaults: CheckConstraints, FireTriggers and
/// KeepIdentity, no TableLock, and the unit of work's transaction when there is one.
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
    /// Translates a BulkInsertCommand to batched T-SQL INSERT statements, the serial route.
    /// </summary>
    public override Task<IGenericResult<SqlCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        MsSqlBulkInsertTranslatorLog.Translating(
            NullLogger<MsSqlBulkInsertTranslator>.Instance, container?.Name ?? "<null>");

        try
        {
            var source = Source(command, container);
            if (source.IsFailure)
                return Task.FromResult(source.ToNewResult<SqlCommand>());

            var (dbPath, collection) = source.Value!;
            return Task.FromResult(GenericResult<SqlCommand>.Success(
                MsSqlBatchInsertTranslator.BuildBatchedInsertStatements(container!, dbPath, collection)));
        }
        catch (Exception ex)
        {
            MsSqlBulkInsertTranslatorLog.BulkInsertTranslationFailed(
                NullLogger<MsSqlBulkInsertTranslator>.Instance, ex, container?.Name ?? "<null>", ex.Message);
            return Task.FromResult(GenericResult<SqlCommand>.Failure(
                MsSqlDataResultCodes.ByName("BulkInsertTranslationFailed"),
                ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    /// <summary>
    /// Plans the bulk copy of a BulkInsertCommand: the rows as a DataTable, the table to copy into and the columns to map.
    /// </summary>
    /// <param name="command">The bulk insert command, carrying its rows as input data.</param>
    /// <param name="container">The container the rows are copied into.</param>
    /// <returns>The plan, or the coded failure that stopped it.</returns>
    public static IGenericResult<MsSqlBulkCopyPlan> Plan(IDataCommand command, IStorageContainer container)
    {
        try
        {
            var source = Source(command, container);
            if (source.IsFailure)
                return source.ToNewResult<MsSqlBulkCopyPlan>();

            var (dbPath, collection) = source.Value!;
            var fields = MsSqlBatchInsertTranslator.InsertableFields(container);
            if (fields.Count == 0)
            {
                MsSqlBulkInsertTranslatorLog.NoInsertableFields(
                    NullLogger<MsSqlBulkInsertTranslator>.Instance, container.Name);
                throw new InvalidOperationException($"Container {container.Name} has no insertable fields");
            }

            var rows = ConvertToDataTable(collection, fields);
            MsSqlBulkInsertTranslatorLog.Planned(
                NullLogger<MsSqlBulkInsertTranslator>.Instance, container.Name, rows.Rows.Count);
            return GenericResult<MsSqlBulkCopyPlan>.Success(
                new MsSqlBulkCopyPlan(rows, BuildSchemaQualifiedTableName(dbPath), [.. fields.Select(f => f.Name)]));
        }
        catch (Exception ex)
        {
            MsSqlBulkInsertTranslatorLog.BulkInsertTranslationFailed(
                NullLogger<MsSqlBulkInsertTranslator>.Instance, ex, container.Name, ex.Message);
            return GenericResult<MsSqlBulkCopyPlan>.Failure(
                MsSqlDataResultCodes.ByName("BulkInsertTranslationFailed"),
                ResultDetails.Create("ErrorMessage", ex.Message));
        }
    }

    // What both routes need first: the database path of the container and the rows the command carries, each refused with its
    // own coded failure when it is missing or not what a bulk insert takes.
    private static IGenericResult<BulkSource> Source(IDataCommand command, IStorageContainer? container)
    {
        if (container == null)
        {
            MsSqlBulkInsertTranslatorLog.ContainerNull(NullLogger<MsSqlBulkInsertTranslator>.Instance);
            return GenericResult<BulkSource>.Failure(MsSqlDataResultCodes.ByName("ContainerNull"));
        }

        if (container.Path is not IDatabasePath dbPath)
        {
            MsSqlBulkInsertTranslatorLog.InvalidContainerPath(
                NullLogger<MsSqlBulkInsertTranslator>.Instance, container.Name);
            return GenericResult<BulkSource>.Failure(MsSqlDataResultCodes.ByName("InvalidContainerPath"));
        }

        var dataObj = GetCommandData(command);
        if (dataObj == null)
        {
            MsSqlBulkInsertTranslatorLog.MissingInputData(
                NullLogger<MsSqlBulkInsertTranslator>.Instance, "BulkInsertCommand", container.Name);
            return GenericResult<BulkSource>.Failure(
                MsSqlDataResultCodes.ByName("MissingInputData"),
                ResultDetails.Create("CommandType", "BulkInsertCommand"));
        }

        if (dataObj is not IEnumerable collection)
        {
            MsSqlBulkInsertTranslatorLog.InvalidDataType(
                NullLogger<MsSqlBulkInsertTranslator>.Instance, container.Name, dataObj.GetType().Name);
            return GenericResult<BulkSource>.Failure(
                MsSqlDataResultCodes.ByName("InvalidDataType"),
                ResultDetails.Create("TranslatorName", "MsSqlBulkInsertTranslator", "ActualType", dataObj.GetType().Name));
        }

        return GenericResult<BulkSource>.Success(new BulkSource(dbPath, collection));
    }

    // The container's database path and the rows the command carries.
    private sealed record BulkSource(IDatabasePath Path, IEnumerable Collection);

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

    // Each column is read from the row the one way the serial route reads it (RowValues), so the copy stores the rows the
    // INSERT statements would have stored. A column the row lacks, or holds null, is a database NULL.
    private static void FillRow(DataRow row, object entity, List<IField> fields)
    {
        foreach (var field in fields)
        {
            RowValues.TryRead(entity, field.Name, out var value, out _);
            row[field.Name] = value ?? DBNull.Value;
        }
    }
}
