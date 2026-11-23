using Fdw.Collections.Attributes;

namespace Fdw.Services.Abstractions;

/// <summary>Nobody was asked: the database enforces the citation, or it cites no domain.</summary>
[TypeOption(typeof(CitedStates), "Unchecked")]
public sealed class UncheckedCitedState : CitedStateBase
{
    /// <summary>Initializes a new instance of the <see cref="UncheckedCitedState"/> class.</summary>
    public UncheckedCitedState()
        : base(4, "Unchecked")
    {
    }
}
