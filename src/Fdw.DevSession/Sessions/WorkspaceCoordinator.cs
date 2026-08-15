using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

/// <summary>Coordinates concurrent strands working inside ONE development session.</summary>
/// <remarks>
/// <para>
/// Distinct from the session manager: that one governs whether a session exists at all (open,
/// sleep, close, dedup by fix key). This one governs concurrent work *within* a single session —
/// several strands (each a resumable agent conversation) editing non-overlapping parts of the same
/// isolated copy at the same time.
/// </para>
/// <para>
/// Fencing is advisory in the sense that nothing physically prevents a writer from ignoring its
/// claim; it is authoritative in the sense that a claim is refused rather than narrowed. Two
/// strands are never granted overlapping paths, so "who may write here" always has one answer.
/// </para>
/// </remarks>
public sealed class WorkspaceCoordinator : IWorkspaceCoordinator
{
    private readonly IDevSessionManager _sessions;
    private readonly IMcpEventBus _bus;
    private readonly ILogger<WorkspaceCoordinator> _logger;

    // Why keyed by session: strands only contend within the session that shares an isolated copy.
    // Two sessions have different worktrees, so identical paths in each are different files.
    // Why the case-insensitive strand key: same reasoning as path fencing — two strand ids that
    // differ only in case are far more likely to be one strand named inconsistently than two
    // genuinely distinct strands, and colliding them refuses a claim rather than double-granting.
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, StrandInfo>> _strands = new();

    /// <summary>Initializes the coordinator.</summary>
    public WorkspaceCoordinator(
        IDevSessionManager sessions,
        IMcpEventBus bus,
        ILogger<WorkspaceCoordinator>? logger = null)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _logger = logger ?? NullLogger<WorkspaceCoordinator>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<ScopeClaim>> FenceStrand(
        Guid sessionId,
        ScopeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var session = _sessions.Get(sessionId);
        if (session.IsFailure) return session.ToNewResult<ScopeClaim>();

        // Why: an empty claim would fence nothing while reading as a granted claim, so every later
        // overlap check would pass and the strand would appear safely scoped when it is not.
        if (request.Paths.Count == 0)
        {
            return GenericResult<ScopeClaim>.Failure(
                WorkspaceCoordinatorLog.EmptyScope(_logger, request.StrandId));
        }

        var active = ActiveState();
        if (active is null)
        {
            return GenericResult<ScopeClaim>.Failure(
                WorkspaceCoordinatorLog.StrandStateNotRegistered(_logger, ActiveStateName));
        }

        var forSession = _strands.GetOrAdd(sessionId, _ => new ConcurrentDictionary<string, StrandInfo>(StringComparer.OrdinalIgnoreCase));

        if (forSession.ContainsKey(request.StrandId))
        {
            return GenericResult<ScopeClaim>.Failure(
                WorkspaceCoordinatorLog.StrandAlreadyFenced(_logger, request.StrandId, sessionId));
        }

        // Why only non-terminal strands contend: a reconciled or abandoned strand has released its
        // claim, so its paths are available again.
        var conflict = forSession.Values.FirstOrDefault(existing =>
            !existing.State.IsTerminal && ScopePaths.Overlap(existing.Claim.Paths, request.Paths));
        if (conflict is not null)
        {
            return GenericResult<ScopeClaim>.Failure(
                WorkspaceCoordinatorLog.ScopeOverlap(_logger, request.StrandId, conflict.StrandId));
        }

        var claim = new ScopeClaim(request.StrandId, sessionId, request.Paths, DateTimeOffset.UtcNow);

        // Why TryAdd rather than an indexer assignment: two callers racing the same strand id must
        // not both believe they hold the claim. The loser is told it is already fenced.
        if (!forSession.TryAdd(request.StrandId, new StrandInfo(request.StrandId, claim, active)))
        {
            return GenericResult<ScopeClaim>.Failure(
                WorkspaceCoordinatorLog.StrandAlreadyFenced(_logger, request.StrandId, sessionId));
        }

        WorkspaceCoordinatorLog.StrandFenced(_logger, request.StrandId, sessionId, request.Paths.Count);
        await Publish(sessionId, request.StrandId, DevSessionTopics.StrandFenced, cancellationToken).ConfigureAwait(false);

        return GenericResult<ScopeClaim>.Success(claim);
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<StrandInfo>>> ListStrands(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = _sessions.Get(sessionId);
        if (session.IsFailure)
        {
            return Task.FromResult(session.ToNewResult<IReadOnlyList<StrandInfo>>());
        }

        IReadOnlyList<StrandInfo> strands = _strands.TryGetValue(sessionId, out var forSession)
            ? forSession.Values.ToArray()
            : [];

        return Task.FromResult(GenericResult<IReadOnlyList<StrandInfo>>.Success(strands));
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Route(
        Guid sessionId,
        StrandInfo strand,
        CancellationToken cancellationToken = default)
    {
        if (strand is null) throw new ArgumentNullException(nameof(strand));

        var session = _sessions.Get(sessionId);
        if (session.IsFailure) return session;

        // Why this can legitimately find nothing: StrandHandlers ships EMPTY on purpose. The
        // framework owns routing; handlers are consumer domain work. An unroutable strand is
        // therefore a real configuration gap and is reported as one rather than silently ignored.
        var handler = StrandHandlers.All().FirstOrDefault(h => h.CanHandle(strand));
        if (handler is null)
        {
            return GenericResult.Failure(
                WorkspaceCoordinatorLog.NoHandlerForStrand(_logger, strand.StrandId));
        }

        var handled = await handler.Handle(session.Value!, strand, cancellationToken).ConfigureAwait(false);
        if (handled.IsFailure) return handled;

        WorkspaceCoordinatorLog.StrandRouted(_logger, strand.StrandId, handler.Name);
        await Publish(sessionId, strand.StrandId, DevSessionTopics.StrandRouted, cancellationToken).ConfigureAwait(false);

        return GenericResult.Success();
    }

    /// <inheritdoc />
    public async Task<IGenericResult<StrandInfo>> Reconcile(
        Guid sessionId,
        string strandId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(strandId)) throw new ArgumentException("Strand id is required.", nameof(strandId));

        var session = _sessions.Get(sessionId);
        if (session.IsFailure) return session.ToNewResult<StrandInfo>();

        if (!_strands.TryGetValue(sessionId, out var forSession)
            || !forSession.TryGetValue(strandId, out var strand))
        {
            return GenericResult<StrandInfo>.Failure(
                WorkspaceCoordinatorLog.StrandNotFound(_logger, strandId, sessionId));
        }

        // Why: reconciling twice would re-release a claim that another strand may already have
        // taken over, so a terminal strand fails loud instead.
        if (strand.State.IsTerminal)
        {
            return GenericResult<StrandInfo>.Failure(
                WorkspaceCoordinatorLog.StrandIsTerminal(_logger, strandId, strand.State.Name));
        }

        var reconciled = StrandStates.ByName(ReconciledStateName);
        if (reconciled == StrandStates.NotFound)
        {
            return GenericResult<StrandInfo>.Failure(
                WorkspaceCoordinatorLog.StrandStateNotRegistered(_logger, ReconciledStateName));
        }

        var updated = new StrandInfo(strand.StrandId, strand.Claim, reconciled);
        forSession[strandId] = updated;

        WorkspaceCoordinatorLog.StrandReconciled(_logger, strandId, sessionId);
        await Publish(sessionId, strandId, DevSessionTopics.StrandReconciled, cancellationToken).ConfigureAwait(false);

        return GenericResult<StrandInfo>.Success(updated);
    }

    private const string ActiveStateName = "Active";
    private const string ReconciledStateName = "Reconciled";

    private static IStrandState? ActiveState()
        => StrandStates.ByName(ActiveStateName) == StrandStates.NotFound
            ? null
            : StrandStates.ByName(ActiveStateName);

    private async Task Publish(Guid sessionId, string strandId, string leaf, CancellationToken cancellationToken)
    {
        var entry = new SessionLedgerEntry(sessionId, leaf, DateTimeOffset.UtcNow)
        {
            StrandId = strandId,
        };

        await _bus.Publish(
            new McpEventDraft(
                DevSessionTopics.For(sessionId, leaf),
                sessionId,
                null,
                ViewIntents.ByName("Silent"),
                nameof(SessionLedgerEntry),
                new ReadOnlyMemory<byte>(JsonSerializer.SerializeToUtf8Bytes(entry))),
            cancellationToken).ConfigureAwait(false);
    }
}
