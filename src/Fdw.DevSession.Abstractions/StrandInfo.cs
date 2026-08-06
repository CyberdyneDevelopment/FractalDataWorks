namespace Fdw.DevSession.Abstractions;

/// <summary>
/// A snapshot of one concurrent strand of work within a session: its granted scope claim and current
/// state. A strand is a parallel line of work (e.g. a side agent handling a non-conflicting aspect)
/// that the coordinator fences, routes, and later reconciles back into the session.
/// </summary>
// Why: pure data holder, no logic beyond trivial construction/assignment
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class StrandInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StrandInfo"/> class.
    /// </summary>
    /// <param name="strandId">The strand's identifier.</param>
    /// <param name="claim">The scope claim the strand holds.</param>
    /// <param name="state">The strand's current state.</param>
    public StrandInfo(string strandId, ScopeClaim claim, IStrandState state)
    {
        StrandId = strandId;
        Claim = claim;
        State = state;
    }

    /// <summary>Gets the strand's identifier.</summary>
    public string StrandId { get; }

    /// <summary>Gets the scope claim the strand holds.</summary>
    public ScopeClaim Claim { get; }

    /// <summary>Gets the strand's current state.</summary>
    public IStrandState State { get; }
}
