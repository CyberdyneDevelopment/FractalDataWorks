using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Mcp.Bus.Abstractions;

/// <summary>
/// MCP event bus contract. Publishers (MCP servers, dispatchers, participants) emit events;
/// sinks (stdio, in-proc canvas, SSE, audit) subscribe by topic pattern. The bus owns durability
/// and replay — there is no separate storage abstraction.
/// </summary>
/// <remarks>
/// <para>
/// Topic patterns use glob-style segments: <c>mssql/*/schema</c>, <c>roslyn/*</c>,
/// <c>pidgin/scene/*</c>. <c>*</c> matches a single segment; <c>**</c> matches zero or more.
/// </para>
/// <para>
/// Replay enumerations are returned in <see cref="McpEvent.EventId"/> order.
/// </para>
/// <para>
/// Why these methods don't return <c>IGenericResult&lt;T&gt;</c>: this is an eventing contract,
/// not an operation contract. Publish failures are infrastructure failures (bus disposed, channel
/// full) that surface as exceptions; subscribers consume an open-ended event stream where wrapping
/// every element in a result would be noise. Domain-level success/failure rides on the
/// <see cref="IViewIntent"/> + topic-phase convention (<c>result</c> vs <c>error</c>).
/// </para>
/// </remarks>
public interface IMcpEventBus
{
    /// <summary>Assigns an EventId + Timestamp and broadcasts to live subscribers and the durable log.</summary>
    ValueTask<ulong> Publish(McpEventDraft draft, CancellationToken cancellationToken = default);

    /// <summary>Live subscription. Yields events as they are published; respects the topic pattern.</summary>
    IAsyncEnumerable<McpEvent> Subscribe(string topicPattern, CancellationToken cancellationToken = default);

    /// <summary>Replays events with EventId &gt;= <paramref name="fromEventId"/> matching the pattern.</summary>
    IAsyncEnumerable<McpEvent> Replay(string topicPattern, ulong fromEventId, CancellationToken cancellationToken = default);

    /// <summary>Replays events whose Timestamp falls inside <c>[from, to]</c> matching the pattern.</summary>
    IAsyncEnumerable<McpEvent> ReplayBetween(string topicPattern, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

    /// <summary>Replays the causation chain rooted at <paramref name="rootEventId"/>, in EventId order.</summary>
    IAsyncEnumerable<McpEvent> ReplayCausation(ulong rootEventId, CancellationToken cancellationToken = default);
}
