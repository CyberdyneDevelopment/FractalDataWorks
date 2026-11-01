using System;
using System.Collections.Generic;

namespace Fdw.Results.Abstractions;

/// <summary>
/// Interface for result details that can be formatted into messages.
/// </summary>
public interface IResultDetails : IDisposable
{
    /// <summary>
    /// Gets the key-value pairs of detail data.
    /// </summary>
    IReadOnlyDictionary<string, object?> Data { get; }

    /// <summary>
    /// Gets a value from the details by key.
    /// </summary>
    /// <typeparam name="T">The expected type of the value.</typeparam>
    /// <param name="key">The key to look up.</param>
    /// <returns>The value if found and of the correct type, otherwise default.</returns>
    T? GetValue<T>(string key);

    /// <summary>
    /// Gets whether this details instance has been returned to the pool.
    /// </summary>
    bool IsPooled { get; }
}
