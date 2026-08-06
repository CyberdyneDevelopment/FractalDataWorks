using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.DevSession.Abstractions;

/// <summary>
/// The strand's work is being merged back into the session (its claimed scope folded in). In flight,
/// not yet terminal.
/// </summary>
[TypeOption(typeof(StrandStates), "Reconciling")]
[ExcludeFromCodeCoverage]
public sealed class ReconcilingStrandState : StrandStateBase
{
    /// <summary>Initializes a new instance of the <see cref="ReconcilingStrandState"/> class.</summary>
    public ReconcilingStrandState() : base(2, "Reconciling", isTerminal: false) { }
}
