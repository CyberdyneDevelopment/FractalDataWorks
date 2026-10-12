using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Intelligence.Logging;
using Fdw.Intelligence.Memory;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Intelligence.Orchestration;

/// <summary>
/// Background worker that monitors agent state and provides semantic recall triggers.
/// Implements the "Deja Vu" pattern from the Mnemosyne architecture.
/// </summary>
public sealed class MemorySidecar
{
    private static readonly string[] Triggers = new[]
    {
        "deja vu",
        "tip of my tongue",
        "missing context",
        "remember something like this"
    };

    private readonly IVectorMemoryStore _memoryStore;
    private readonly ILogger<MemorySidecar> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemorySidecar"/> class.
    /// </summary>
    /// <param name="memoryStore">The vector memory store.</param>
    /// <param name="logger">The logger instance.</param>
    public MemorySidecar(IVectorMemoryStore memoryStore, ILogger<MemorySidecar>? logger = null)
    {
        _memoryStore = memoryStore ?? throw new ArgumentNullException(nameof(memoryStore));
        _logger = logger ?? NullLogger<MemorySidecar>.Instance;
    }

    /// <summary>
    /// Processes a stream of terminal output and looks for recall triggers.
    /// When a trigger phrase is detected, queries the memory store for related context.
    /// </summary>
    /// <param name="streamContent">The content to evaluate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the recalled context, or null if no trigger detected.</returns>
    public async Task<IGenericResult<string?>> Evaluate(string streamContent, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(streamContent))
        {
            return GenericResult<string?>.Success(null);
        }

        if (!Triggers.Any(t => streamContent.Contains(t, StringComparison.OrdinalIgnoreCase)))
        {
            return GenericResult<string?>.Success(null);
        }

        IntelligenceLog.TriggerDetected(_logger, streamContent);

        var query = ExtractQuery(streamContent);
        var memoriesResult = await _memoryStore.Recall(query, 3).ConfigureAwait(false);

        if (!memoriesResult.IsSuccess || memoriesResult.Value == null || memoriesResult.Value.Count == 0)
        {
            return GenericResult<string?>.Success(null);
        }

        var response = "[SYSTEM RECALL]: Based on your thought, I found these related items in your project memory:\n" +
                       string.Join("\n---\n", memoriesResult.Value.Select(m => m.Content));

        IntelligenceLog.RecallInjected(_logger, memoriesResult.Value.Count);
        return GenericResult<string?>.Success(response);
    }

    private static string ExtractQuery(string streamContent)
    {
        var words = streamContent.Split(' ');
        return words.Length > 0 ? words[words.Length - 1] : streamContent;
    }
}
