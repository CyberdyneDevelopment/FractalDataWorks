using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// The moment of a pipeline run at which a Notify stage fires: after its stage, after the load, or when the run fails (FDW-1120).
/// </summary>
public interface IRunPoint : ITypeOption<int, IRunPoint>
{
    /// <summary>Gets whether the notification fires once, after the final page of the run has passed the stage.</summary>
    bool FiresAfterStage { get; }

    /// <summary>Gets whether the notification fires once, after the run's load has committed.</summary>
    bool FiresAfterLoad { get; }

    /// <summary>Gets whether the notification fires once, when the run fails.</summary>
    bool FiresOnFailure { get; }
}
