using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Abstractions;

/// <summary>Claimed by a host that intends to run this phase itself. The collect skips it; the next explicit call runs it.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PhaseStates), "Deferred")]
public sealed class DeferredPhaseState : PhaseStateBase
{
    /// <summary>Initializes a new instance of the <see cref="DeferredPhaseState"/> class.</summary>
    public DeferredPhaseState()
        : base(2, "Deferred", hasRun: false, isDeferred: true)
    {
    }
}
