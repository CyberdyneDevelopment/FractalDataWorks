using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// What happens to a run of a pipeline triggered while as many runs of that pipeline are active as it may have at once: Queue, Refuse or DelayedStart (FDW-3117, FDW-3129).
/// </summary>
public interface IOverflowBehavior : ITypeOption<int, IOverflowBehavior>
{
    /// <summary>Gets whether the run is refused. When false the run waits for its turn.</summary>
    bool RefusesWhenFull { get; }

    /// <summary>Gets whether the run is held until a delay has passed or a time of day comes, and then waits its turn (FDW-3129).</summary>
    bool HoldsWhenFull { get; }
}
