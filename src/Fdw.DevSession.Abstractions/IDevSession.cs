using System;

namespace Fdw.DevSession.Abstractions;

/// <summary>
/// A single unit of development work — a fix, thread, or conversation — administered from open to done.
/// A dev session is bound to the <see cref="Key"/> (the fix/issue/conversation it exists to resolve),
/// NOT to the human or agent working it: either can attach to or detach from it, and its warm context
/// and intent ledger survive across those attachments. Physically it is a branch (and optional working
/// tree) plus a replayable ledger, so it rides on git and is portable.
/// </summary>
public interface IDevSession
{
    /// <summary>Gets the session's stable identifier.</summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the key of the fix/issue/conversation this session exists to resolve. Sessions are deduplicated
    /// by this key: opening a session for a key that already has a live session attaches to that session.
    /// </summary>
    string Key { get; }

    /// <summary>Gets the isolated working copy this session operates in.</summary>
    IsolatedCopy Copy { get; }

    /// <summary>Gets the session's current lifecycle state.</summary>
    ISessionState State { get; }

    /// <summary>
    /// Gets the identifier of the parent session when this is a nested session (a side issue handled while
    /// the parent is held), or <see langword="null"/> for a top-level session.
    /// </summary>
    Guid? ParentSessionId { get; }

    /// <summary>Gets the instant the session was opened.</summary>
    DateTimeOffset OpenedAt { get; }

    /// <summary>
    /// Gets the instant of the session's most recent activity. Used to decide when a dormant session may be
    /// reclaimed (a session blocked on a slow human is free to reclaim).
    /// </summary>
    DateTimeOffset LastActiveAt { get; }
}
