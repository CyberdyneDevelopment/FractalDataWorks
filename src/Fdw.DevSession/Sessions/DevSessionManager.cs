using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.DevSession.Abstractions;
using Fdw.DevSession.Logging;
using Fdw.Mcp.Bus;
using Fdw.Mcp.Bus.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.DevSession.Sessions;

/// <summary>Opens, suspends and closes development sessions.</summary>
/// <remarks>
/// <para>
/// A session is keyed to the FIX, not the worker. Humans are expensive to spawn and carry durable
/// intent; agents are cheap to spawn and carry none — so the warm context and the intent ledger
/// attach to the work itself, and either kind of worker can pick it up. That is why
/// <see cref="Open"/> deduplicates by <see cref="SessionRequest.Key"/> and returns the existing
/// session rather than materializing a second isolated copy for the same fix.
/// </para>
/// <para>
/// The in-memory registry is a cache, not the system of record: the durable state of a session is
/// its branch and worktree on disk, and its history is the ledger replayed from
/// <see cref="IMcpEventBus"/>. Reconstructing the registry from those two sources after a restart
/// is a separate cut and is deliberately not faked here — nothing in this class pretends a session
/// survives the process.
/// </para>
/// </remarks>
public sealed class DevSessionManager : IDevSessionManager
{
    private readonly IWorktreeEngine _engine;
    private readonly IMcpEventBus _bus;
    private readonly ILogger<DevSessionManager> _logger;
    private readonly ConcurrentDictionary<Guid, DevSession> _sessions = new();

    /// <summary>Initializes the manager.</summary>
    public DevSessionManager(
        IWorktreeEngine engine,
        IMcpEventBus bus,
        ILogger<DevSessionManager>? logger = null)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _logger = logger ?? NullLogger<DevSessionManager>.Instance;
    }

    /// <inheritdoc />
    public Task<IGenericResult<IDevSession>> Open(
        SessionRequest request,
        CancellationToken cancellationToken = default)
        => OpenCore(request, parentSessionId: null, cancellationToken);

    /// <inheritdoc />
    public async Task<IGenericResult<IDevSession>> OpenNested(
        Guid parentSessionId,
        SessionRequest request,
        CancellationToken cancellationToken = default)
    {
        // Why: the parent is resolved before any git work happens, so a bad parent id never leaves
        // an orphaned branch or worktree behind.
        var parent = Get(parentSessionId);
        if (parent.IsFailure) return parent;

        return await OpenCore(request, parentSessionId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public IGenericResult<IDevSession> Get(Guid sessionId)
        => _sessions.TryGetValue(sessionId, out var session)
            ? GenericResult<IDevSession>.Success(session)
            : GenericResult<IDevSession>.Failure(DevSessionManagerLog.SessionNotFoundById(_logger, sessionId));

    /// <inheritdoc />
    public IGenericResult<IDevSession> Get(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required.", nameof(key));

        var match = FindLiveByKey(key);
        return match is null
            ? GenericResult<IDevSession>.Failure(DevSessionManagerLog.SessionNotFoundByKey(_logger, key))
            : GenericResult<IDevSession>.Success(match);
    }

    /// <inheritdoc />
    public IReadOnlyList<IDevSession> List() => _sessions.Values.ToArray();

    /// <inheritdoc />
    public Task<IGenericResult<IDevSession>> Sleep(Guid sessionId, CancellationToken cancellationToken = default)
        => Transition(sessionId, SleepingStateName, DevSessionTopics.Slept, "slept", cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<IDevSession>> Wake(Guid sessionId, CancellationToken cancellationToken = default)
        => Transition(sessionId, OpenStateName, DevSessionTopics.Woke, "woken", cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<IDevSession>> Close(Guid sessionId, CancellationToken cancellationToken = default)
        => Transition(sessionId, DoneStateName, DevSessionTopics.Closed, "closed", cancellationToken);

    private const string OpenStateName = "Open";
    private const string SleepingStateName = "Sleeping";
    private const string DoneStateName = "Done";

    private async Task<IGenericResult<IDevSession>> OpenCore(
        SessionRequest request,
        Guid? parentSessionId,
        CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        // Why: dedup happens before materializing anything. Opening the same fix twice must not
        // create a second branch — the second caller joins the session that already exists.
        var existing = FindLiveByKey(request.Key);
        if (existing is not null)
        {
            DevSessionManagerLog.SessionReused(_logger, existing.Id, request.Key);
            return GenericResult<IDevSession>.Success(existing);
        }

        var isolationLevel = IsolationLevels.ByName(request.IsolationLevelName);
        if (isolationLevel == IsolationLevels.NotFound)
        {
            return GenericResult<IDevSession>.Failure(
                DevSessionManagerLog.UnknownIsolationLevel(_logger, request.IsolationLevelName));
        }

        var openState = SessionStates.ByName(OpenStateName);
        if (openState == SessionStates.NotFound)
        {
            return GenericResult<IDevSession>.Failure(
                DevSessionManagerLog.StateNotRegistered(_logger, OpenStateName));
        }

        var materialized = await isolationLevel
            .Materialize(_engine, request.Isolation, cancellationToken)
            .ConfigureAwait(false);
        if (materialized.IsFailure)
        {
            // Why: the engine's own failure is preserved as the inner result rather than replaced,
            // so the caller still sees git's reason (bad base ref, existing branch, and so on).
            return materialized.ToNewResult<IDevSession>();
        }

        var session = new DevSession(
            Guid.NewGuid(),
            request.Key,
            materialized.Value!,
            openState,
            DateTimeOffset.UtcNow,
            parentSessionId);
        _sessions[session.Id] = session;

        if (parentSessionId is null)
        {
            DevSessionManagerLog.SessionOpened(_logger, session.Id, session.Key, session.Copy.BranchName);
            await PublishLifecycle(session, DevSessionTopics.Opened, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            DevSessionManagerLog.NestedSessionOpened(_logger, session.Id, parentSessionId.Value, session.Key);
            await PublishLifecycle(session, DevSessionTopics.NestedOpened, cancellationToken).ConfigureAwait(false);
        }

        return GenericResult<IDevSession>.Success(session);
    }

    private async Task<IGenericResult<IDevSession>> Transition(
        Guid sessionId,
        string targetStateName,
        string topicLeaf,
        string attemptedOperation,
        CancellationToken cancellationToken)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            return GenericResult<IDevSession>.Failure(
                DevSessionManagerLog.SessionNotFoundById(_logger, sessionId));
        }

        // Why: a terminal session is finished. Re-opening or re-sleeping one would resurrect work
        // whose branch may already have been merged and pruned, so it fails loud instead.
        if (session.State.IsTerminal)
        {
            return GenericResult<IDevSession>.Failure(
                DevSessionManagerLog.SessionIsTerminal(_logger, sessionId, session.State.Name, attemptedOperation));
        }

        // Why: waking something that was never asleep is a caller bug, not a no-op to absorb.
        if (string.Equals(targetStateName, OpenStateName, StringComparison.Ordinal)
            && !string.Equals(session.State.Name, SleepingStateName, StringComparison.Ordinal))
        {
            return GenericResult<IDevSession>.Failure(
                DevSessionManagerLog.InvalidTransition(_logger, sessionId, session.State.Name, attemptedOperation));
        }

        var targetState = SessionStates.ByName(targetStateName);
        if (targetState == SessionStates.NotFound)
        {
            return GenericResult<IDevSession>.Failure(
                DevSessionManagerLog.StateNotRegistered(_logger, targetStateName));
        }

        var previousStateName = session.State.Name;
        session.TransitionTo(targetState, DateTimeOffset.UtcNow);
        DevSessionManagerLog.SessionTransitioned(_logger, sessionId, previousStateName, targetState.Name);

        await PublishLifecycle(session, topicLeaf, cancellationToken).ConfigureAwait(false);

        return GenericResult<IDevSession>.Success(session);
    }

    // Why: the ledger IS the bus. Every lifecycle change is published under dev/session/<id>/<leaf>
    // so a subscriber can follow a live session and a late joiner can replay the same topic to
    // reconstruct its history — there is no second, divergent audit store.
    private async Task PublishLifecycle(DevSession session, string leaf, CancellationToken cancellationToken)
    {
        var entry = new SessionLedgerEntry(session.Id, leaf, session.LastActiveAt)
        {
            Detail = session.Copy.BranchName,
        };

        await _bus.Publish(
            new McpEventDraft(
                DevSessionTopics.For(session.Id, leaf),
                session.Id,
                null,
                ViewIntents.ByName("Silent"),
                nameof(SessionLedgerEntry),
                new ReadOnlyMemory<byte>(JsonSerializer.SerializeToUtf8Bytes(entry))),
            cancellationToken).ConfigureAwait(false);
    }

    // Why: "live" excludes terminal sessions so a key can be reused after its session is closed;
    // otherwise a fix could never be reopened after being finished once.
    private DevSession? FindLiveByKey(string key)
        => _sessions.Values.FirstOrDefault(s =>
            string.Equals(s.Key, key, StringComparison.Ordinal) && !s.State.IsTerminal);
}
