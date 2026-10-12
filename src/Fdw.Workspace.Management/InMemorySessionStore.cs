using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Workspace.Roslyn.Results;

namespace Fdw.Workspace.Management;

/// <summary>
/// In-memory implementation of <see cref="IWorkspaceSessionStore"/> for testing
/// and scenarios where persistence is not required.
/// </summary>
public sealed class InMemorySessionStore : IWorkspaceSessionStore
{
    private readonly ConcurrentDictionary<Guid, WorkspaceSession> _sessions = new();

    /// <inheritdoc/>
    public Task<IGenericResult<bool>> Save(WorkspaceSession session, CancellationToken cancellationToken = default)
    {
        _sessions[session.Id] = session;
        return Task.FromResult(GenericResult<bool>.Success(true));
    }

    /// <inheritdoc/>
    public Task<IGenericResult<WorkspaceSession>> Load(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
            return Task.FromResult(GenericResult<WorkspaceSession>.Success(session));

        return Task.FromResult(GenericResult<WorkspaceSession>.Failure(
            WorkspaceResultCodes.ByName("SessionNotFound"),
            ResultDetails.Create("SessionId", sessionId)));
    }

    /// <inheritdoc/>
    public Task<IGenericResult<bool>> Delete(Guid sessionId, CancellationToken cancellationToken = default)
    {
        _sessions.TryRemove(sessionId, out _);
        return Task.FromResult(GenericResult<bool>.Success(true));
    }

    /// <inheritdoc/>
    public Task<IEnumerable<SessionInfo>> List(CancellationToken cancellationToken = default)
    {
        var sessions = _sessions.Values
            .Select(s => new SessionInfo
            {
                Id = s.Id,
                OriginalWorkspaceId = s.WorkspaceId,
                SolutionPath = s.SolutionPath,
                Name = s.Name,
                SavedAt = s.SavedAt,
                SnapshotCount = s.Snapshots.Count,
                HasBaseline = s.BaselineSnapshot is not null
            })
            .OrderByDescending(s => s.SavedAt);

        return Task.FromResult<IEnumerable<SessionInfo>>(sessions.ToList());
    }

    /// <inheritdoc/>
    public Task<bool> Exists(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_sessions.ContainsKey(sessionId));
    }

    /// <summary>
    /// Clears all sessions from the store.
    /// </summary>
    public void Clear() => _sessions.Clear();

    /// <summary>
    /// Gets the number of sessions in the store.
    /// </summary>
    public int Count => _sessions.Count;
}
