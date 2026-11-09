using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fdw.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Rows;
using Fdw.Data.MsSql.Logging;
using Fdw.Data.MsSql.Results;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// The one refusal of a value a SQL Server date column cannot take (FDW-4726): <c>MSSQL-21030</c>, naming the container, the column and the kind of value, never the value. The bulk plan, the
/// serial insert and the serial update all refuse through it, so a value fails the same way however the rows travel and before it reaches the database.
/// </summary>
internal static class MsSqlDateValues
{
    /// <summary>The type a date column takes (DateOnly or DateTime), or null when the field is not a date field. A field typed object whose type is named DateTime is a DateTime column, as the bulk plan makes it.</summary>
    /// <param name="field">The field.</param>
    /// <returns>The column type, or null.</returns>
    internal static Type? DateColumnType(IField field)
    {
        var clr = field.FieldType.ClrType.WithoutNullable();
        if (clr == typeof(DateOnly) || clr == typeof(DateTime))
        {
            return clr;
        }

        return clr == typeof(object) && string.Equals(field.FieldType.TypeName, "DateTime", StringComparison.Ordinal) ? typeof(DateTime) : null;
    }

    /// <summary>The coded failure for a value a date column cannot take; logs it too.</summary>
    /// <param name="container">The container name.</param>
    /// <param name="column">The column name.</param>
    /// <param name="value">The value: only its kind is reported.</param>
    /// <returns>The failure, code InvalidDateValue.</returns>
    internal static IGenericResult Invalid(string container, string column, object value)
    {
        var kind = value.GetType().Name;
        MsSqlInsertTranslatorLog.InvalidDateValue(NullLogger<MsSqlInsertTranslator>.Instance, container, column, kind);
        return GenericResult.Failure(
            MsSqlDataResultCodes.ByName("InvalidDateValue"),
            ResultDetails.Create("Container", container, "Column", column, "ValueKind", kind));
    }

    /// <summary>The refusal of the first value in <paramref name="rows"/> that a date field in <paramref name="fields"/> cannot take, or null when every value fits.</summary>
    /// <param name="container">The container name.</param>
    /// <param name="fields">The fields the statement writes.</param>
    /// <param name="rows">The rows (entities or dictionaries) being written.</param>
    /// <returns>The failure, or null.</returns>
    internal static IGenericResult? Refusal(string container, IEnumerable<IField> fields, IEnumerable rows)
    {
        var dateFields = new List<(IField Field, Type Column)>();
        foreach (var field in fields)
        {
            if (DateColumnType(field) is { } column)
            {
                dateFields.Add((field, column));
            }
        }

        if (dateFields.Count == 0)
        {
            return null;
        }

        foreach (var row in rows)
        {
            foreach (var (field, column) in dateFields)
            {
                if (RowValues.TryRead(row, field.Name, out var value, out _) && value is not null && !MsSqlBulkCopyColumnValue.TryFit(column, value, out _))
                {
                    return Invalid(container, field.Name, value);
                }
            }
        }

        return null;
    }

    /// <summary>The refusal of the first value in <paramref name="rows"/> that a date column among <paramref name="columns"/> cannot take (an upsert's write columns), or null.</summary>
    /// <param name="container">The container.</param>
    /// <param name="columns">The names of the columns the statement writes.</param>
    /// <param name="rows">The rows being written.</param>
    /// <returns>The failure, or null.</returns>
    internal static IGenericResult? Refusal(IStorageContainer container, IReadOnlyCollection<string> columns, IEnumerable rows)
        => Refusal(container.Name, container.Schema.Fields.Where(f => columns.Contains(f.Name, StringComparer.Ordinal)), rows);

    /// <summary>
    /// The refusal of the first filter value a date column cannot be compared with, or null. A pattern match is left alone (its value is text by design), and so is any value that is
    /// not plain (text, a number or a flag): a subquery or a reference is not a date to refuse.
    /// </summary>
    /// <param name="container">The container name.</param>
    /// <param name="fields">The container's fields.</param>
    /// <param name="filter">The filter, or null.</param>
    /// <returns>The failure, or null.</returns>
    internal static IGenericResult? Refusal(string container, IEnumerable<IField> fields, IFilterExpression? filter)
    {
        if (filter?.Root is null)
        {
            return null;
        }

        var dates = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in fields)
        {
            if (DateColumnType(field) is { } column)
            {
                dates[field.Name] = column;
            }
        }

        return dates.Count == 0 ? null : Node(container, filter.Root, dates);
    }

    private static IGenericResult? Node(string container, IFilterNode node, Dictionary<string, Type> dates)
    {
        if (node is FilterGroup group)
        {
            foreach (var child in group.Nodes)
            {
                if (Node(container, child, dates) is { } refused)
                {
                    return refused;
                }
            }

            return null;
        }

        return node is FilterCondition condition ? Condition(container, condition, dates) : null;
    }

    private static IGenericResult? Condition(string container, FilterCondition condition, Dictionary<string, Type> dates)
    {
        if (condition.Operator.IsPatternMatch || !condition.Operator.RequiresValue || condition.Value is null || !dates.TryGetValue(condition.PropertyName, out var column))
        {
            return null;
        }

        var values = condition.Value is IEnumerable list and not string and not byte[] ? list.Cast<object?>() : new[] { condition.Value };
        foreach (var value in values)
        {
            if (value is not null && IsPlain(value) && !MsSqlBulkCopyColumnValue.TryFit(column, value, out _))
            {
                return Invalid(container, condition.PropertyName, value);
            }
        }

        return null;
    }

    private static bool IsPlain(object value) => value is string or decimal || value.GetType().IsPrimitive;
}
