using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.DevSession.Abstractions;

/// <summary>
/// The strand's work has been merged back into the session and its scope claim released. Terminal.
/// </summary>
[TypeOption(typeof(StrandStates), "Reconciled")]
[ExcludeFromCodeCoverage]
public sealed class ReconciledStrandState : StrandStateBase
{
    /// <summary>Initializes a new instance of the <see cref="ReconciledStrandState"/> class.</summary>
    public ReconciledStrandState() : base(3, "Reconciled", isTerminal: true) { }
}
