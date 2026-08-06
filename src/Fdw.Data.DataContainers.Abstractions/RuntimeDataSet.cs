using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;

namespace Fdw.Data.DataContainers.Abstractions;

/// <summary>
/// In-memory implementation of IRuntimeDataSet.
/// Stores all rows in a list for efficient random access and multiple passes.
/// Supports success/failure states for railway-oriented programming.
/// </summary>
public class RuntimeDataSet : IRuntimeDataSet
{
    private readonly IDataSchema _schema;
    private readonly IEnumerable<IDataRow> _rows;
    private readonly bool _isSuccess;
    private readonly string? _errorMessage;

    /// <summary>
    /// Creates a runtime dataset.
    /// </summary>
    private RuntimeDataSet(string name, IDataSchema schema, IEnumerable<IDataRow> rows, bool isSuccess, string? message = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        _rows = rows?.ToList() ?? throw new ArgumentNullException(nameof(rows));
        _isSuccess = isSuccess;
        _errorMessage = message;
    }

    /// <summary>
    /// Gets whether this dataset is successful (has valid data).
    /// </summary>
    public bool IsSuccess => _isSuccess;

    /// <summary>
    /// Gets the error message for failed datasets.
    /// </summary>
    public string? ErrorMessage => _errorMessage;

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public IDataSchema Schema => _schema;

    /// <inheritdoc/>
    public int RowCount => IsSuccess ? _rows.Count() : 0;

    /// <inheritdoc/>
    public IEnumerable<IDataRow> Rows => IsSuccess ? _rows : Enumerable.Empty<IDataRow>();

    /// <inheritdoc/>
    public IDataRow GetRow(int index)
    {
        if (!IsSuccess)
            throw new InvalidOperationException($"Cannot access rows on failed dataset: {ErrorMessage}");

        var list = _rows.ToList();
        if (index < 0 || index >= list.Count)
            throw new ArgumentOutOfRangeException(nameof(index), index, $"Index {index} out of range [0, {list.Count})");

        return list[index];
    }

    /// <inheritdoc/>
    public IRuntimeDataSet Where(Func<IDataRow, bool> predicate)
    {
        return IsSuccess
            ? new RuntimeDataSet(Name, Schema, _rows.Where(predicate), true)
            : this;
    }

    /// <inheritdoc/>
    public IRuntimeDataSet Select(Func<IDataRow, IDataRow> selector)
    {
        return IsSuccess
            ? new RuntimeDataSet(Name, Schema, _rows.Select(selector), true)
            : this;
    }

    /// <inheritdoc/>
    public decimal Sum(string fieldName)
    {
        if (!IsSuccess)
            throw new InvalidOperationException($"Cannot sum failed dataset: {ErrorMessage}");

        return _rows.Sum(row => row.GetValue<decimal>(fieldName));
    }

    /// <inheritdoc/>
    public decimal Average(string fieldName)
    {
        if (!IsSuccess)
            throw new InvalidOperationException($"Cannot average failed dataset: {ErrorMessage}");

        return _rows.Average(row => row.GetValue<decimal>(fieldName));
    }

    /// <inheritdoc/>
    public decimal Min(string fieldName)
    {
        if (!IsSuccess)
            throw new InvalidOperationException($"Cannot find min on failed dataset: {ErrorMessage}");

        return _rows.Min(row => row.GetValue<decimal>(fieldName));
    }

    /// <inheritdoc/>
    public decimal Max(string fieldName)
    {
        if (!IsSuccess)
            throw new InvalidOperationException($"Cannot find max on failed dataset: {ErrorMessage}");

        return _rows.Max(row => row.GetValue<decimal>(fieldName));
    }

    /// <inheritdoc/>
    public int Count() => IsSuccess ? _rows.Count() : 0;

    /// <inheritdoc/>
    public int Count(Func<IDataRow, bool> predicate) => IsSuccess ? _rows.Count(predicate) : 0;

    /// <inheritdoc/>
    public IEnumerable<IGrouping<object?, IDataRow>> GroupBy(string fieldName)
    {
        return IsSuccess
            ? _rows.GroupBy(row => row.GetValue(fieldName))
            : Enumerable.Empty<IGrouping<object?, IDataRow>>();
    }

    /// <inheritdoc/>
    public IRuntimeDataSet OrderBy(string fieldName, bool descending = false)
    {
        if (!IsSuccess)
            return this;

        var ordered = descending
            ? _rows.OrderByDescending(row => row.GetValue(fieldName))
            : _rows.OrderBy(row => row.GetValue(fieldName));

        return new RuntimeDataSet(Name, Schema, ordered, true);
    }

    /// <inheritdoc/>
    public IRuntimeDataSet Take(int count)
    {
        return IsSuccess
            ? new RuntimeDataSet(Name, Schema, _rows.Take(count), true)
            : this;
    }

    /// <inheritdoc/>
    public IRuntimeDataSet Skip(int count)
    {
        return IsSuccess
            ? new RuntimeDataSet(Name, Schema, _rows.Skip(count), true)
            : this;
    }

    /// <inheritdoc/>
    public IDataRow? FirstOrDefault()
    {
        return IsSuccess ? _rows.FirstOrDefault() : null;
    }

    /// <inheritdoc/>
    public IDataRow? FirstOrDefault(Func<IDataRow, bool> predicate)
    {
        return IsSuccess ? _rows.FirstOrDefault(predicate) : null;
    }

    /// <inheritdoc/>
    public IReadOnlyList<IDataRow> ToList()
    {
        return IsSuccess ? _rows.ToList() : [];
    }

    /// <inheritdoc/>
    public IDataRow[] ToArray()
    {
        return IsSuccess ? _rows.ToArray() : [];
    }

    /// <summary>
    /// Creates a successful empty dataset with the specified schema.
    /// </summary>
    public static RuntimeDataSet Empty(string name, IDataSchema schema)
    {
        return new RuntimeDataSet(name, schema, [], true);
    }

    /// <summary>
    /// Creates a successful dataset from a list of rows.
    /// </summary>
    public static RuntimeDataSet FromRows(string name, IDataSchema schema, params IDataRow[] rows)
    {
        return new RuntimeDataSet(name, schema, rows, true);
    }

    /// <summary>
    /// Creates a failed dataset with error message.
    /// </summary>
    public static RuntimeDataSet Failure(string name, string errorMessage)
    {
        return new RuntimeDataSet(name, DataSchema.Empty(), [], false, errorMessage);
    }

    /// <summary>
    /// Creates a successful dataset.
    /// </summary>
    public static RuntimeDataSet Success(string name, IDataSchema schema, IEnumerable<IDataRow> rows)
    {
        return new RuntimeDataSet(name, schema, rows, true);
    }
}
