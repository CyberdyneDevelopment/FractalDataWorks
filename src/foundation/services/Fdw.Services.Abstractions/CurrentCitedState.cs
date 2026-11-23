using Fdw.Collections.Attributes;

namespace Fdw.Services.Abstractions;

/// <summary>A current version of the member is held.</summary>
[TypeOption(typeof(CitedStates), "Current")]
public sealed class CurrentCitedState : CitedStateBase
{
    /// <summary>Initializes a new instance of the <see cref="CurrentCitedState"/> class.</summary>
    public CurrentCitedState()
        : base(3, "Current")
    {
    }
}
