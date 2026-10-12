using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Intelligence.Logging;
using Fdw.Intelligence.Memory;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Intelligence.Memory;

/// <summary>
/// In-memory implementation of the vector memory store.
/// Uses keyword matching for recall (prototype — will be replaced by vector similarity).
/// </summary>
public sealed class VectorMemoryStore : IVectorMemoryStore
{
    private readonly ConcurrentDictionary<Guid, VectorMemoryEntry> _store = new();
    private readonly ILogger<VectorMemoryStore> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VectorMemoryStore"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public VectorMemoryStore(ILogger<VectorMemoryStore>? logger = null)
    {
        _logger = logger ?? NullLogger<VectorMemoryStore>.Instance;
    }

    /// <inheritdoc />
    public Task<IGenericResult<VectorMemoryEntry?>> Get(Guid key, CancellationToken ct = default)
    {
        _store.TryGetValue(key, out var entry);
        return Task.FromResult(GenericResult<VectorMemoryEntry?>.Success(entry));
    }

    /// <inheritdoc />
    public Task<IGenericResult> Set(Guid key, VectorMemoryEntry value, CancellationToken ct = default)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        _store[key] = value;
        IntelligenceLog.MemoryRecorded(_logger, key);
        return Task.FromResult(GenericResult.Success());
    }

    /// <inheritdoc />
    public Task<IGenericResult> Remove(Guid key, CancellationToken ct = default)
    {
        _store.TryRemove(key, out _);
        return Task.FromResult(GenericResult.Success());
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<VectorMemoryEntry>>> Get(CancellationToken ct = default)
    {
        var entries = _store.Values.ToList().AsReadOnly();
        return Task.FromResult(GenericResult<IReadOnlyList<VectorMemoryEntry>>.Success(entries));
    }

    /// <inheritdoc />
    public Task<IGenericResult> Record(string content, IReadOnlyDictionary<string, string>? metadata = null)
    {
        var id = Guid.NewGuid();
        var entry = new VectorMemoryEntry
        {
            Id = id,
            Content = content,
            Timestamp = DateTimeOffset.UtcNow,
            Metadata = metadata ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        };

        return Set(id, entry);
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<VectorMemoryEntry>>> Recall(string query, int limit = 5)
    {
        var results = _store.Values
            .Where(m => m.Content.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(m => m.Timestamp)
            .Take(limit)
            .ToList()
            .AsReadOnly();

        IntelligenceLog.MemoryRecalled(_logger, query, results.Count);
        return Task.FromResult(GenericResult<IReadOnlyList<VectorMemoryEntry>>.Success(results));
    }
}
