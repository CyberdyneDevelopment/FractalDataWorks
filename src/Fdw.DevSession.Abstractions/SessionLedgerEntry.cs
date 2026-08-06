using System;

namespace Fdw.DevSession.Abstractions;

/// <summary>
/// One durable entry in a session's intent/activity ledger. The ledger is the session's externalized
/// memory: it is what a worker replays to attach with full context, and each entry is published as an
/// event on the realtime bus (topic <see cref="Topic"/>). Implementations both persist entries and
/// publish them; the ledger is the replay of these events. This type is deliberately transport-free so
/// publisher and subscriber share one shape without coupling the abstractions to a bus.
/// </summary>
// Why: pure data holder, no logic beyond trivial construction/assignment
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class SessionLedgerEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionLedgerEntry"/> class.
    /// </summary>
    /// <param name="sessionId">The session the entry belongs to.</param>
    /// <param name="topic">The realtime-bus topic the entry is published under (see <see cref="DevSessionTopics"/>).</param>
    /// <param name="at">The instant the entry was recorded.</param>
    public SessionLedgerEntry(Guid sessionId, string topic, DateTimeOffset at)
    {
        SessionId = sessionId;
        Topic = topic;
        At = at;
    }

    /// <summary>Gets the session the entry belongs to.</summary>
    public Guid SessionId { get; }

    /// <summary>Gets the realtime-bus topic the entry is published under.</summary>
    public string Topic { get; }

    /// <summary>Gets the instant the entry was recorded.</summary>
    public DateTimeOffset At { get; }

    /// <summary>Gets the identifier of the strand the entry concerns, when it is strand-scoped; otherwise null.</summary>
    public string? StrandId { get; init; }

    /// <summary>Gets optional human-readable detail describing what the entry records.</summary>
    public string? Detail { get; init; }
}
