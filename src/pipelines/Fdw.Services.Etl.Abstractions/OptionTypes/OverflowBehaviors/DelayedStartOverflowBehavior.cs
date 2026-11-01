using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The run is held until a set delay has passed or a set time of day comes, then waits its turn like any other run.</summary>
[TypeOption(typeof(OverflowBehaviors), "DelayedStart")]
public sealed class DelayedStartOverflowBehavior : OverflowBehaviorBase
{
    /// <summary>Initializes a new instance of the <see cref="DelayedStartOverflowBehavior"/> class.</summary>
    public DelayedStartOverflowBehavior() : base(
        "DelayedStart",
        "The run is held until a set delay has passed or a set time of day comes, then waits its turn like any other run.",
        refusesWhenFull: false,
        holdsWhenFull: true)
    {
    }
}
