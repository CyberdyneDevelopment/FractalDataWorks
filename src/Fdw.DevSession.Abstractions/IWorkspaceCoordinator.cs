using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.DevSession.Abstractions;

/// <summary>
/// Coordinates concurrent strands of work WITHIN a single dev session (as distinct from managing the
/// session's existence, which is <see cref="IDevSessionManager"/>'s job). It grants each strand a
/// non-overlapping scope claim, routes strands to their handlers, and reconciles finished strands back
/// into the session. This is what lets a side agent work a non-conflicting aspect in parallel and then
/// fold that work back cleanly.
/// </summary>
public interface IWorkspaceCoordinator
{
    /// <summary>
    /// Fences a strand by granting it an exclusive, non-overlapping scope claim over part of the session's
    /// working copy. Fails loud (no partial or overlapping grant) when the requested paths intersect a
    /// claim already held by another live strand in the same session.
    /// </summary>
    /// <param name="sessionId">The session the strand belongs to.</param>
    /// <param name="request">The scope the strand is requesting.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the granted <see cref="ScopeClaim"/>, or a failure when it would overlap.</returns>
    Task<IGenericResult<ScopeClaim>> FenceStrand(Guid sessionId, ScopeRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the live strands in a session.
    /// </summary>
    /// <param name="sessionId">The session whose strands to list.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the session's strands, or a failure when the session is unknown.</returns>
    Task<IGenericResult<IReadOnlyList<StrandInfo>>> ListStrands(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Routes a fenced strand to the first <see cref="IStrandHandler"/> that can handle it and runs it.
    /// Fails loud when no registered handler accepts the strand.
    /// </summary>
    /// <param name="sessionId">The session the strand belongs to.</param>
    /// <param name="strand">The strand to route.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating whether the routed handler's work succeeded.</returns>
    Task<IGenericResult> Route(Guid sessionId, StrandInfo strand, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reconciles a finished strand back into the session — folding its claimed scope in and releasing the
    /// claim — and transitions the strand to a terminal state.
    /// </summary>
    /// <param name="sessionId">The session the strand belongs to.</param>
    /// <param name="strandId">The strand to reconcile.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the reconciled <see cref="StrandInfo"/>, or a failure.</returns>
    Task<IGenericResult<StrandInfo>> Reconcile(Guid sessionId, string strandId, CancellationToken cancellationToken = default);
}
