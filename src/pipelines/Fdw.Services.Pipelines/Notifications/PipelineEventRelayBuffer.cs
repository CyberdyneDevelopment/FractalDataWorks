using System;
using System.Collections.Generic;
using System.Threading;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Pipelines.Notifications;

/// <summary>
/// The events a host that serves no hub has handed to its relay and not yet sent (FDW-5249): one per host, filled by the relay's client and
/// emptied once per tick by <see cref="PipelineEventRelayFlusher"/>.
/// </summary>
/// <remarks>
/// <para>
/// Events leave in the order they arrived. A components update replaces the one still waiting for the same run, so a tick carries the latest
/// figures of each run once, and the replacement takes the later place. Every other kind (status, progress, completion, pause, resume, task and
/// edge) is kept as it came: none is merged and none is dropped.
/// </para>
/// <para>
/// The capacity bounds what the buffer holds. Over it, the oldest components update is dropped, with a coded warning, until it fits or none
/// is left. Events of the other kinds are admitted beyond the capacity: they are a handful for each run, and losing one would leave a run
/// showing the wrong state.
/// </para>
/// </remarks>
public sealed class PipelineEventRelayBuffer
{
    private readonly Lock _gate = new();
    private readonly LinkedList<PipelineEventRequest> _events = new();
    private readonly Dictionary<Guid, LinkedListNode<PipelineEventRequest>> _pendingComponents = [];
    private readonly int _capacity;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="PipelineEventRelayBuffer"/> class.</summary>
    /// <param name="capacity">How many events the buffer holds before it drops its oldest components update; at least one.</param>
    /// <param name="logger">The logger.</param>
    public PipelineEventRelayBuffer(int capacity, ILogger<PipelineEventRelayBuffer>? logger = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _capacity = capacity;
        _logger = logger ?? NullLogger<PipelineEventRelayBuffer>.Instance;
    }

    /// <summary>Gets how many events are waiting.</summary>
    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _events.Count;
            }
        }
    }

    /// <summary>Adds an event to the end of the buffer, replacing the waiting components update of the same run when it is one.</summary>
    /// <param name="request">The event.</param>
    public void Add(PipelineEventRequest request)
    {
        lock (_gate)
        {
            if (ComponentsExecution(request) is { } executionId)
            {
                if (_pendingComponents.Remove(executionId, out var older))
                {
                    _events.Remove(older);
                }

                _pendingComponents[executionId] = _events.AddLast(request);
            }
            else
            {
                _ = _events.AddLast(request);
            }

            while (_events.Count > _capacity && OldestPendingComponents() is { } oldest)
            {
                DropComponents(oldest);
            }
        }
    }

    /// <summary>Takes every waiting event, oldest first, and leaves the buffer empty.</summary>
    /// <returns>The events.</returns>
    public IReadOnlyList<PipelineEventRequest> Drain()
    {
        lock (_gate)
        {
            List<PipelineEventRequest> drained = [.. _events];
            _events.Clear();
            _pendingComponents.Clear();
            return drained;
        }
    }

    // The run a components update is about; null for any other event, which is never merged.
    private static Guid? ComponentsExecution(PipelineEventRequest request)
        => string.Equals(request.Kind, PipelineEventKinds.ComponentsChanged.Name, StringComparison.Ordinal) && request.Components is { } update
            ? update.ExecutionId
            : null;

    // The waiting components update that arrived first, or null when none is waiting. Each drop removes one node, so the loop that uses it ends.
    private LinkedListNode<PipelineEventRequest>? OldestPendingComponents()
    {
        for (var node = _events.First; node is not null; node = node.Next)
        {
            if (ComponentsExecution(node.Value) is { } executionId
                && _pendingComponents.TryGetValue(executionId, out var pending)
                && ReferenceEquals(pending, node))
            {
                return node;
            }
        }

        return null;
    }

    private void DropComponents(LinkedListNode<PipelineEventRequest> node)
    {
        if (ComponentsExecution(node.Value) is { } executionId)
        {
            _ = _pendingComponents.Remove(executionId);
            PipelineEventRelayLog.RelayBufferFull(_logger, _capacity, executionId);
        }

        _events.Remove(node);
    }
}
