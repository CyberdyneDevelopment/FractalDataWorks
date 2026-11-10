using System;
using System.Collections.Generic;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// The key-value details of a result. Every <see cref="Create()"/> returns a new instance that is never recycled (FDW-4785): results share
/// their details (a copied result holds its source's), so an instance a result can still hold must never be cleared or handed to another result.
/// </summary>
public sealed class ResultDetails : IResultDetails
{
    private readonly Dictionary<string, object?> _data = new(StringComparer.Ordinal);

    private ResultDetails()
    {
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Data => _data;

    /// <inheritdoc />
    /// <remarks>Always <see langword="false"/>: instances are not pooled (FDW-4785).</remarks>
    public bool IsPooled => false;

    /// <summary>
    /// Creates a new, empty <see cref="ResultDetails"/> instance.
    /// </summary>
    /// <returns>A new <see cref="ResultDetails"/> instance.</returns>
    public static ResultDetails Create() => new();

    /// <summary>
    /// Creates a <see cref="ResultDetails"/> instance with a single key-value pair.
    /// </summary>
    public static ResultDetails Create(string key, object? value)
    {
        var instance = Create();
        instance._data[key] = value;
        return instance;
    }

    /// <summary>
    /// Creates a <see cref="ResultDetails"/> instance with two key-value pairs.
    /// </summary>
    public static ResultDetails Create(string key1, object? value1, string key2, object? value2)
    {
        var instance = Create();
        instance._data[key1] = value1;
        instance._data[key2] = value2;
        return instance;
    }

    /// <summary>
    /// Creates a <see cref="ResultDetails"/> instance with three key-value pairs.
    /// </summary>
    public static ResultDetails Create(string key1, object? value1, string key2, object? value2, string key3, object? value3)
    {
        var instance = Create();
        instance._data[key1] = value1;
        instance._data[key2] = value2;
        instance._data[key3] = value3;
        return instance;
    }

    /// <summary>
    /// Adds a key-value pair to the details.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public ResultDetails With(string key, object? value)
    {
        _data[key] = value;
        return this;
    }

    /// <inheritdoc />
    public T? GetValue<T>(string key)
    {
        if (_data.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }

        return default;
    }

    /// <summary>
    /// Does nothing. The data stays intact and the instance is never recycled, because a result may still hold it (FDW-4785).
    /// </summary>
    public void Dispose()
    {
    }
}
