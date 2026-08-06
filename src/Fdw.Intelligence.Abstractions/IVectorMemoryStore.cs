using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Intelligence.Memory;

/// <summary>
/// Defines a semantic vector memory store for agent context.
/// Extends the base memory store with semantic search capabilities.
/// </summary>
public interface IVectorMemoryStore : IMemoryStore<Guid, VectorMemoryEntry>
{
    /// <summary>
    /// Records a new vector memory entry with optional metadata.
    /// </summary>
    /// <param name="content">The content to remember.</param>
    /// <param name="metadata">Optional metadata associated with the memory.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<IGenericResult> Record(string content, IReadOnlyDictionary<string, string>? metadata = null);

    /// <summary>
    /// Recalls vector memories similar to the specified query using semantic search.
    /// </summary>
    /// <param name="query">The search query or concept.</param>
    /// <param name="limit">The maximum number of memories to return.</param>
    /// <returns>A result containing the matching memory entries.</returns>
    Task<IGenericResult<IReadOnlyList<VectorMemoryEntry>>> Recall(string query, int limit = 5);
}
