using System;
using Fdw.DevSession.Abstractions;

namespace Fdw.DevSession.Sessions;

/// <summary>A development session: an isolated copy plus the state it is currently in.</summary>
/// <remarks>
/// State is mutable because a session is a long-lived entity that transitions in place — the manager
/// owns every transition, which is why the setter is internal rather than public.
/// </remarks>
internal sealed class DevSession : IDevSession
{
    public DevSession(
        Guid id,
        string key,
        IsolatedCopy copy,
        ISessionState state,
        DateTimeOffset openedAt,
        Guid? parentSessionId)
    {
        Id = id;
        Key = key;
        Copy = copy;
        State = state;
        OpenedAt = openedAt;
        LastActiveAt = openedAt;
        ParentSessionId = parentSessionId;
    }

    public Guid Id { get; }

    public string Key { get; }

    public IsolatedCopy Copy { get; }

    public ISessionState State { get; private set; }

    public Guid? ParentSessionId { get; }

    public DateTimeOffset OpenedAt { get; }

    public DateTimeOffset LastActiveAt { get; private set; }

    /// <summary>Moves the session to a new state and stamps it active.</summary>
    internal void TransitionTo(ISessionState state, DateTimeOffset at)
    {
        State = state;
        LastActiveAt = at;
    }
}
