using Fdw.Collections.Attributes;

namespace Fdw.Services.Abstractions;

/// <summary>A version of the member is held, and none is current: it was retired or deleted.</summary>
[TypeOption(typeof(CitedStates), "Retired")]
public sealed class RetiredCitedState : CitedStateBase
{
    /// <summary>Initializes a new instance of the <see cref="RetiredCitedState"/> class.</summary>
    public RetiredCitedState()
        : base(2, "Retired")
    {
    }
}
