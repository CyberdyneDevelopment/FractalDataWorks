using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Fdw.Abstractions;
using Fdw.Commands.Data.Abstractions;
using Fdw.Conventions;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Rows;
using Fdw.Data.MsSql.Logging;
using Fdw.Data.MsSql.Results;
using Fdw.Results;
using Fdw.Services.Connections.Sql;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.MsSql;

public sealed partial class MsSqlInsertTranslator
{
    /// <summary>
    /// Plans the SqlBulkCopy of an insert's rows: the same rows and the same columns the serial statements would write.
    /// </summary>
    /// <param name="command">The insert command.</param>
    /// <param name="container">The container the rows go into.</param>
    /// <returns>The copy plan, or the coded failure that says why the rows cannot be copied.</returns>
    public static IGenericResult<MsSqlBulkCopyPlan> Plan(IDataCommand command, IStorageContainer container)
    {
        try
        {
            var source = Source(command, container);
            if (source.IsFailure)
                return source.ToNewResult<MsSqlBulkCopyPlan>();

            var (dbPath, collection) = source.Value!;
            var insertable = InsertableFields(container);
            if (insertable.Count == 0)
            {
                MsSqlInsertTranslatorLog.NoInsertableFields(
                    NullLogger<MsSqlInsertTranslator>.Instance, container.Name);
                throw new InvalidOperationException($"Container {container.Name} has no insertable fields");
            }

            var entities = collection.Cast<object>().ToList();

            // No rows plan an empty table with every insertable column; rows plan the columns they supply.
            var fields = entities.Count == 0 ? insertable : InsertColumns.Supplied(insertable, entities);
            if (fields.Count == 0)
            {
                MsSqlInsertTranslatorLog.NoMatchingProperties(
                    NullLogger<MsSqlInsertTranslator>.Instance, container.Name);
                throw new InvalidOperationException(
                    $"The rows have no properties matching insertable fields for container {container.Name}");
            }

            var table = ConvertToDataTable(entities, fields, container.Name);
            if (table.IsFailure)
                return table.ToNewResult<MsSqlBulkCopyPlan>();

            var rows = table.Value!;
            MsSqlInsertTranslatorLog.Planned(
                NullLogger<MsSqlInsertTranslator>.Instance, container.Name, rows.Rows.Count);

            return GenericResult<MsSqlBulkCopyPlan>.Success(
                new MsSqlBulkCopyPlan(rows, BuildSchemaQualifiedTableName(dbPath), [.. fields.Select(f => f.Name)]));
        }
        catch (Exception ex)
        {
            MsSqlInsertTranslatorLog.InsertTranslationFailed(
                NullLogger<MsSqlInsertTranslator>.Instance, ex, container.Name, ex.Message);
            return GenericResult<MsSqlBulkCopyPlan>.Failure(
                MsSqlDataResultCodes.ByName("InsertTranslationFailed"),
                ResultDetails.Create("ErrorMessage", ex.Message));
        }
    }

    [ConventionOverride(MaxCyclomaticComplexity = 20)]  // DataTable conversion with type inference from field metadata and switch-based type mapping
    internal static IGenericResult<DataTable> ConvertToDataTable(IEnumerable collection, List<IField> fields, string container)
    {
        var dataTable = new DataTable();

        foreach (var field in fields)
        {
            var clrType = field.FieldType.ClrType;
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

            var columnType = field.IsNullable && clrType.IsValueType
                ? typeof(Nullable<>).MakeGenericType(clrType)
                : clrType;
            dataTable.Columns.Add(field.Name, columnType.WithoutNullable());
        }

        foreach (var entity in collection)
        {
            var row = dataTable.NewRow();
            var filled = FillRow(row, entity, fields, container);
            if (filled.IsFailure)
                return filled.ToNewResult<DataTable>();

            dataTable.Rows.Add(row);
        }

        return GenericResult<DataTable>.Success(dataTable);
    }

    // Each value goes in as its column's type; a value a date column cannot take (text that is not a date) refuses the plan with its own code, naming the column and the kind of value.
    private static IGenericResult FillRow(DataRow row, object entity, List<IField> fields, string container)
    {
        foreach (var field in fields)
        {
            RowValues.TryRead(entity, field.Name, out var value, out _);
            var column = row.Table.Columns[field.Name]!;
            if (value is null)
            {
                row[column] = DBNull.Value;
                continue;
            }

            if (!MsSqlBulkCopyColumnValue.TryFit(column.DataType, value, out var fitted))
                return MsSqlDateValues.Invalid(container, field.Name, value);

            row[column] = fitted;
        }

        return GenericResult.Success();
    }
}
