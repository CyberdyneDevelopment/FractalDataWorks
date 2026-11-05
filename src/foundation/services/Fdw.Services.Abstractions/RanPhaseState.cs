using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Abstractions;

/// <summary>The phase has run. Nothing runs it again without <c>force</c>. Set even when the phase failed: leaving it unset would let a later collect run it a second time on top of whatever the first attempt already did.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PhaseStates), "Ran")]
public sealed class RanPhaseState : PhaseStateBase
{
    /// <summary>Initializes a new instance of the <see cref="RanPhaseState"/> class.</summary>
    public RanPhaseState()
        : base(3, "Ran", hasRun: true, isDeferred: false)
    {
    }
}
