using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Abstractions;

/// <summary>The phase has not run. A collect or an explicit call will run it.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PhaseStates), "NotRun")]
public sealed class NotRunPhaseState : PhaseStateBase
{
    /// <summary>Initializes a new instance of the <see cref="NotRunPhaseState"/> class.</summary>
    public NotRunPhaseState()
        : base(1, "NotRun", hasRun: false, isDeferred: false)
    {
    }
}
