using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// Pooled implementation of <see cref="IResultDetails"/> for efficient allocation.
/// </summary>
public sealed class ResultDetails : IResultDetails
{
    private static readonly ConcurrentBag<ResultDetails> Pool = new();
    private static readonly int MaxPoolSize = 100;

    private readonly Dictionary<string, object?> _data = new(StringComparer.Ordinal);
    private bool _isPooled;

    private ResultDetails()
    {
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Data => _data;

    /// <inheritdoc />
    public bool IsPooled => _isPooled;

    /// <summary>
    /// Gets a <see cref="ResultDetails"/> instance from the pool or creates a new one.
    /// </summary>
    /// <returns>A <see cref="ResultDetails"/> instance.</returns>
    public static ResultDetails Create()
    {
        if (Pool.TryTake(out var instance))
        {
            instance._isPooled = false;
            return instance;
        }

        return new ResultDetails();
    }

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
        if (_isPooled)
        {
            throw new ObjectDisposedException(nameof(ResultDetails), "Cannot modify a pooled instance.");
        }

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

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isPooled)
        {
            return;
        }

        _data.Clear();
        _isPooled = true;

        if (Pool.Count < MaxPoolSize)
        {
            Pool.Add(this);
        }
    }
}
