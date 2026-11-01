using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Mcp.Bus;
using Fdw.Mcp.Bus.Abstractions;

namespace Fdw.DevSession.Tests;

/// <summary>An <see cref="IMcpEventBus"/> that records what was published.</summary>
/// <remarks>
/// The ledger is the bus, so "did this session record its history" is only answerable by inspecting
/// what reached the bus. A recording double keeps that assertion honest without standing up a real
/// broker, and the replay methods are unsupported rather than faked because nothing under test uses
/// them — a fake replay would be untested behaviour pretending to be a contract.
/// </remarks>
internal sealed class RecordingEventBus : IMcpEventBus
{
    private readonly List<McpEventDraft> _published = [];
    private ulong _nextId;

    public IReadOnlyList<McpEventDraft> Published => _published;

    public ValueTask<ulong> Publish(McpEventDraft draft, CancellationToken cancellationToken = default)
    {
        _published.Add(draft);
        return new ValueTask<ulong>(++_nextId);
    }

    public IAsyncEnumerable<McpEvent> Subscribe(string topicPattern, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Subscribe is not exercised by these tests.");

    public IAsyncEnumerable<McpEvent> Replay(string topicPattern, ulong fromEventId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Replay is not exercised by these tests.");

    public IAsyncEnumerable<McpEvent> ReplayBetween(string topicPattern, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("ReplayBetween is not exercised by these tests.");

    public IAsyncEnumerable<McpEvent> ReplayCausation(ulong rootEventId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("ReplayCausation is not exercised by these tests.");
}
