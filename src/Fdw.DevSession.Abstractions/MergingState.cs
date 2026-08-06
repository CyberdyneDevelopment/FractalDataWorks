using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.DevSession.Abstractions;

/// <summary>
/// The session's work is being submitted and merged back — its strands reconciled and the isolated copy
/// pushed for integration. In flight, so not reclaimable; not yet terminal.
/// </summary>
[TypeOption(typeof(SessionStates), "Merging")]
[ExcludeFromCodeCoverage]
public sealed class MergingState : SessionStateBase
{
    /// <summary>Initializes a new instance of the <see cref="MergingState"/> class.</summary>
    public MergingState() : base(5, "Merging", isTerminal: false, isReclaimable: false) { }
}
