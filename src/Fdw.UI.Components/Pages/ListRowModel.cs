using System;
using System.Collections.Generic;
using Fdw.UI.Abstractions.Pages;

namespace Fdw.UI.Components.Pages;

/// <summary>
/// Concrete implementation of a list row model.
/// </summary>
public sealed class ListRowModel : IListRowModel
{
    private readonly Dictionary<string, object?> _values = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public object Id { get; set; } = "";

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Values => _values;

    /// <inheritdoc />
    public bool IsSelectable { get; set; } = true;

    /// <inheritdoc />
    public IRowStatus Status { get; set; } = RowStatuses.Normal;

    /// <summary>
    /// Sets a cell value.
    /// </summary>
    public void SetValue(string columnId, object? value) => _values[columnId] = value;

    /// <summary>
    /// Gets a cell value.
    /// </summary>
    public T? GetValue<T>(string columnId) =>
        _values.TryGetValue(columnId, out var value) && value is T typed ? typed : default;
}