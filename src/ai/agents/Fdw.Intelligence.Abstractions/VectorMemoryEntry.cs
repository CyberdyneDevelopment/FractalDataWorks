using System;
using System.Collections.Generic;

namespace Fdw.Intelligence.Memory;

/// <summary>
/// Represents a single entry in semantic vector memory.
/// </summary>
public sealed class VectorMemoryEntry
{
    /// <summary>
    /// Gets or sets the unique entry identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the content of the memory.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the memory was recorded.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the metadata associated with this entry.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
