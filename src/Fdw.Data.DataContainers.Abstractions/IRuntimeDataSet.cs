using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;

namespace Fdw.Data.DataContainers.Abstractions;

/// <summary>
/// Represents an in-memory dataset with LINQ-like query operations.
/// </summary>
public interface IRuntimeDataSet
{
    /// <summary>
    /// Gets the name of this dataset.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the schema for this dataset.
    /// </summary>
    IDataSchema Schema { get; }

    /// <summary>
    /// Gets whether this dataset is successful (has valid data).
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Gets the error message for failed datasets.
    /// </summary>
    string? ErrorMessage { get; }

    /// <summary>
    /// Gets the number of rows in the dataset.
    /// </summary>
    int RowCount { get; }

    /// <summary>
    /// Gets all rows in the dataset.
    /// </summary>
    IEnumerable<IDataRow> Rows { get; }

    /// <summary>
    /// Gets a specific row by index.
    /// </summary>
    IDataRow GetRow(int index);

    /// <summary>
    /// Filters rows based on a predicate.
    /// </summary>
    IRuntimeDataSet Where(Func<IDataRow, bool> predicate);

    /// <summary>
    /// Projects rows using a selector.
    /// </summary>
    IRuntimeDataSet Select(Func<IDataRow, IDataRow> selector);

    /// <summary>
    /// Sums values in the specified field.
    /// </summary>
    decimal Sum(string fieldName);

    /// <summary>
    /// Calculates average of values in the specified field.
    /// </summary>
    decimal Average(string fieldName);

    /// <summary>
    /// Finds minimum value in the specified field.
    /// </summary>
    decimal Min(string fieldName);

    /// <summary>
    /// Finds maximum value in the specified field.
    /// </summary>
    decimal Max(string fieldName);

    /// <summary>
    /// Counts all rows.
    /// </summary>
    int Count();

    /// <summary>
    /// Counts rows matching a predicate.
    /// </summary>
    int Count(Func<IDataRow, bool> predicate);

    /// <summary>
    /// Groups rows by the specified field.
    /// </summary>
    IEnumerable<IGrouping<object?, IDataRow>> GroupBy(string fieldName);

    /// <summary>
    /// Orders rows by the specified field.
    /// </summary>
    IRuntimeDataSet OrderBy(string fieldName, bool descending = false);

    /// <summary>
    /// Takes the first N rows.
    /// </summary>
    IRuntimeDataSet Take(int count);

    /// <summary>
    /// Skips the first N rows.
    /// </summary>
    IRuntimeDataSet Skip(int count);

    /// <summary>
    /// Gets the first row or null.
    /// </summary>
    IDataRow? FirstOrDefault();

    /// <summary>
    /// Gets the first row matching predicate or null.
    /// </summary>
    IDataRow? FirstOrDefault(Func<IDataRow, bool> predicate);

    /// <summary>
    /// Converts to list.
    /// </summary>
    IReadOnlyList<IDataRow> ToList();

    /// <summary>
    /// Converts to array.
    /// </summary>
    IDataRow[] ToArray();
}
