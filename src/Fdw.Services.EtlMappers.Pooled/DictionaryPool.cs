using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Fdw.Services.EtlMappers;
using Fdw.Services;
using Fdw;

namespace Fdw.Services.EtlMappers.Pooled;

/// <summary>
/// Thread-safe object pool for dictionaries to eliminate per-row allocations.
/// </summary>
public sealed class DictionaryPool
{
    private readonly ConcurrentBag<Dictionary<string, object?>> _pool = new();
    private readonly int _maxPoolSize;
    private readonly int _maxDictionarySize;

    /// <summary>
    /// Initializes a new instance of the <see cref="DictionaryPool"/> class.
    /// </summary>
    /// <param name="maxPoolSize">Maximum number of dictionaries to pool.</param>
    /// <param name="maxDictionarySize">Maximum dictionary size to pool.</param>
    public DictionaryPool(int maxPoolSize = 1000, int maxDictionarySize = 100)
    {
        _maxPoolSize = maxPoolSize;
        _maxDictionarySize = maxDictionarySize;
    }

    /// <summary>
    /// Rents a dictionary from the pool, or creates a new one if pool is empty.
    /// </summary>
    /// <param name="capacity">The expected capacity for the dictionary.</param>
    /// <returns>A cleared dictionary ready for use.</returns>
    public IDictionary<string, object?> Rent(int capacity)
    {
        if (_pool.TryTake(out var dict))
        {
            dict.Clear();
            return dict;
        }

        return new Dictionary<string, object?>(capacity, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns a dictionary to the pool for reuse.
    /// </summary>
    /// <param name="dict">The dictionary to return.</param>
    public void Return(IDictionary<string, object?> dict)
    {
        if (dict is not Dictionary<string, object?> concrete)
            return;

        // Don't pool oversized dictionaries
        if (concrete.Count > _maxDictionarySize)
            return;

        // Don't exceed pool size
        if (_pool.Count >= _maxPoolSize)
            return;

        concrete.Clear();
        _pool.Add(concrete);
    }

    /// <summary>
    /// Gets the current pool size.
    /// </summary>
    public int CurrentPoolSize => _pool.Count;

    /// <summary>
    /// Clears the pool.
    /// </summary>
    public void Clear()
    {
        while (_pool.TryTake(out _)) { }
    }
}
