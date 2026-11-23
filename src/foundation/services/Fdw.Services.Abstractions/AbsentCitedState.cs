using Fdw.Collections.Attributes;

namespace Fdw.Services.Abstractions;

/// <summary>No version of the member is held: it was never there, or the caller cannot see it.</summary>
[TypeOption(typeof(CitedStates), "Absent")]
public sealed class AbsentCitedState : CitedStateBase
{
    /// <summary>Initializes a new instance of the <see cref="AbsentCitedState"/> class.</summary>
    public AbsentCitedState()
        : base(1, "Absent")
    {
    }
}
