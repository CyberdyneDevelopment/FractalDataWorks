using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Mcp.Bus.Abstractions;

namespace Fdw.Mcp.Bus;

/// <summary>
/// In-process MCP event bus. Holds events in a bounded ring for live subscribers and replay.
/// Durable backing (segmented-file, gateway-backed) is layered as a wrapping decorator that
/// also implements <see cref="IMcpEventBus"/> and forwards Publish to this instance.
/// </summary>
/// <remarks>
/// The ring is the authoritative live store. Replay beyond the ring window is served by a
/// durable sink that subscribes to all events at construction and replays from its log when
/// asked.
/// </remarks>
public sealed class InMemoryMcpEventBus : IMcpEventBus
{
    private readonly ILogger<InMemoryMcpEventBus> _logger;
    private readonly Lock _gate = new();
    private readonly LinkedList<McpEvent> _ring = new();
    private readonly int _ringCapacity;
    private readonly List<Subscription> _subscribers = new();
    private ulong _nextEventId = 1;

    /// <summary>Initializes a new in-memory bus with the given ring capacity.</summary>
    /// <param name="ringCapacity">Max number of events retained for live-window replay. Defaults to 10,000.</param>
    /// <param name="logger">Optional logger; defaults to <see cref="NullLogger{T}"/>.</param>
    public InMemoryMcpEventBus(int ringCapacity = 10_000, ILogger<InMemoryMcpEventBus>? logger = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ringCapacity);
        _ringCapacity = ringCapacity;
        _logger = logger ?? NullLogger<InMemoryMcpEventBus>.Instance;
    }

    /// <inheritdoc />
    public ValueTask<ulong> Publish(McpEventDraft draft, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);

        McpEvent evt;
        Subscription[] snapshot;

        // Why: hold the lock only long enough to assign EventId, append to the ring, and
        // snapshot the subscriber list. Fanning out to channels happens outside the lock so
        // a slow subscriber can never block a publisher.
        lock (_gate)
        {
            evt = new McpEvent(
                EventId: _nextEventId++,
                Topic: draft.Topic,
                Timestamp: DateTimeOffset.UtcNow,
                CorrelationId: draft.CorrelationId,
                Causation: draft.Causation,
                View: draft.View,
                PayloadType: draft.PayloadType,
                Payload: draft.Payload);

            _ring.AddLast(evt);
            if (_ring.Count > _ringCapacity) _ring.RemoveFirst();

            snapshot = _subscribers.ToArray();
        }

        foreach (var sub in snapshot)
        {
            if (McpTopicPattern.Matches(sub.Pattern, evt.Topic))
            {
                // Unbounded channel; TryWrite cannot fail unless completed.
                _ = sub.Channel.Writer.TryWrite(evt);
            }
        }

        McpBusLog.EventPublished(_logger, evt.EventId, evt.Topic);
        return ValueTask.FromResult(evt.EventId);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<McpEvent> Subscribe(string topicPattern, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(topicPattern);

        // Why: register the subscription eagerly — async iterator bodies don't run until
        // MoveNextAsync, which would race with publishers. Do the registration here so the
        // returned enumerator is already wired to the live channel.
        var channel = Channel.CreateUnbounded<McpEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });
        var sub = new Subscription(topicPattern, channel);
        lock (_gate) _subscribers.Add(sub);

        return ConsumeChannel(sub, cancellationToken);
    }

    private async IAsyncEnumerable<McpEvent> ConsumeChannel(Subscription sub, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var evt in sub.Channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                yield return evt;
        }
        finally
        {
            lock (_gate) _subscribers.Remove(sub);
            sub.Channel.Writer.TryComplete();
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<McpEvent> Replay(string topicPattern, ulong fromEventId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(topicPattern);

        foreach (var evt in Snapshot())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (evt.EventId >= fromEventId && McpTopicPattern.Matches(topicPattern, evt.Topic))
                yield return evt;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<McpEvent> ReplayBetween(string topicPattern, DateTimeOffset from, DateTimeOffset to, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(topicPattern);

        foreach (var evt in Snapshot())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (evt.Timestamp >= from && evt.Timestamp <= to && McpTopicPattern.Matches(topicPattern, evt.Topic))
                yield return evt;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<McpEvent> ReplayCausation(ulong rootEventId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chain = new HashSet<ulong> { rootEventId };
        foreach (var evt in Snapshot())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (evt.EventId == rootEventId || (evt.Causation is { } c && chain.Contains(c)))
            {
                chain.Add(evt.EventId);
                yield return evt;
            }
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private McpEvent[] Snapshot()
    {
        lock (_gate)
        {
            var arr = new McpEvent[_ring.Count];
            var i = 0;
            foreach (var e in _ring) arr[i++] = e;
            return arr;
        }
    }

    private sealed record Subscription(string Pattern, Channel<McpEvent> Channel);
}
