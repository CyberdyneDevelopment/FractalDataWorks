using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw;

/// <summary>
/// Defines a basic generic memory store for managing state in-memory.
/// </summary>
/// <typeparam name="TKey">The type of the key.</typeparam>
/// <typeparam name="TValue">The type of the value.</typeparam>
public interface IMemoryStore<TKey, TValue>
    where TKey : notnull
{
    /// <summary>
    /// Gets a value from the store by its key.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the value if found.</returns>
    Task<IGenericResult<TValue?>> Get(TKey key, CancellationToken ct = default);

    /// <summary>
    /// Sets or updates a value in the store.
    /// </summary>
    /// <param name="key">The key to set.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<IGenericResult> Set(TKey key, TValue value, CancellationToken ct = default);

    /// <summary>
    /// Removes a value from the store.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<IGenericResult> Remove(TKey key, CancellationToken ct = default);

    /// <summary>
    /// Gets all values currently in the store.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of all stored values.</returns>
    Task<IGenericResult<IReadOnlyList<TValue>>> Get(CancellationToken ct = default);
}
