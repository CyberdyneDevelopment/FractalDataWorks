using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.DevSession.Abstractions;

/// <summary>
/// The realtime-bus topic contract for dev-session lifecycle events. Every session/strand transition is
/// published under a topic below so subscribers (canvas, dashboards, other agents) get the transition in
/// realtime and the session ledger is the replay of these events. Kept as a shared string contract so
/// publisher and subscriber agree without either depending on the bus.
/// </summary>
[ExcludeFromCodeCoverage]
public static class DevSessionTopics
{
    /// <summary>The topic root for all dev-session events.</summary>
    public const string Root = "dev/session";

    /// <summary>A glob that subscribes to every dev-session event.</summary>
    public const string All = Root + "/**";

    /// <summary>Leaf: a session was opened.</summary>
    public const string Opened = "opened";

    /// <summary>Leaf: a nested session was opened under a parent.</summary>
    public const string NestedOpened = "nested-opened";

    /// <summary>Leaf: a session transitioned state.</summary>
    public const string Transitioned = "transitioned";

    /// <summary>Leaf: a session was put to sleep.</summary>
    public const string Slept = "slept";

    /// <summary>Leaf: a session was woken.</summary>
    public const string Woke = "woke";

    /// <summary>Leaf: a session was closed.</summary>
    public const string Closed = "closed";

    /// <summary>Leaf: a strand was fenced (granted a scope claim).</summary>
    public const string StrandFenced = "strand-fenced";

    /// <summary>Leaf: a strand was routed to a handler.</summary>
    public const string StrandRouted = "strand-routed";

    /// <summary>Leaf: a strand was reconciled back into its session.</summary>
    public const string StrandReconciled = "strand-reconciled";

    /// <summary>
    /// Builds the topic for a specific session and leaf event, e.g. <c>dev/session/{sessionId}/opened</c>.
    /// </summary>
    /// <param name="sessionId">The session the event concerns.</param>
    /// <param name="leaf">The leaf event name (one of the constants on this type).</param>
    /// <returns>The fully-qualified topic string.</returns>
    public static string For(Guid sessionId, string leaf) => Root + "/" + sessionId + "/" + leaf;

    /// <summary>
    /// Builds a glob that subscribes to every event for a specific session, e.g. <c>dev/session/{sessionId}/*</c>.
    /// </summary>
    /// <param name="sessionId">The session to subscribe to.</param>
    /// <returns>The per-session subscription glob.</returns>
    public static string ForSession(Guid sessionId) => Root + "/" + sessionId + "/*";
}
