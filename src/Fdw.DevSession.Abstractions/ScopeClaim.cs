using System;
using System.Collections.Generic;

namespace Fdw.DevSession.Abstractions;

/// <summary>
/// A granted, non-overlapping claim over a slice of a session's working copy. The coordinator issues one
/// per strand and guarantees the claimed paths do not overlap any other live claim in the same session,
/// so strands can proceed in parallel without stepping on each other.
/// </summary>
// Why: pure data holder, no logic beyond trivial construction/assignment
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class ScopeClaim
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScopeClaim"/> class.
    /// </summary>
    /// <param name="strandId">The identifier of the strand that holds the claim.</param>
    /// <param name="sessionId">The identifier of the session the claim belongs to.</param>
    /// <param name="paths">The repo-relative paths granted exclusively to the strand.</param>
    /// <param name="grantedAt">The instant the claim was granted.</param>
    public ScopeClaim(string strandId, Guid sessionId, IReadOnlyList<string> paths, DateTimeOffset grantedAt)
    {
        StrandId = strandId;
        SessionId = sessionId;
        Paths = paths;
        GrantedAt = grantedAt;
    }

    /// <summary>Gets the identifier of the strand that holds the claim.</summary>
    public string StrandId { get; }

    /// <summary>Gets the identifier of the session the claim belongs to.</summary>
    public Guid SessionId { get; }

    /// <summary>Gets the repo-relative paths granted exclusively to the strand.</summary>
    public IReadOnlyList<string> Paths { get; }

    /// <summary>Gets the instant the claim was granted.</summary>
    public DateTimeOffset GrantedAt { get; }
}
