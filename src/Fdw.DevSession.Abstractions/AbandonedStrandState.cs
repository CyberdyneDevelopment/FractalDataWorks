using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.DevSession.Abstractions;

/// <summary>
/// The strand was abandoned: its scope claim was released without merging its work back. Terminal.
/// </summary>
[TypeOption(typeof(StrandStates), "Abandoned")]
[ExcludeFromCodeCoverage]
public sealed class AbandonedStrandState : StrandStateBase
{
    /// <summary>Initializes a new instance of the <see cref="AbandonedStrandState"/> class.</summary>
    public AbandonedStrandState() : base(4, "Abandoned", isTerminal: true) { }
}
