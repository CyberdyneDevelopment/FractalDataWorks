using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Whether a pipeline or a schedule states thresholds on waiting runs of its own, states none, or takes those of the level above it (FDW-3161).
/// </summary>
public interface IWaitThresholdMode : ITypeOption<int, IWaitThresholdMode>
{
    /// <summary>Gets whether the level takes the thresholds of the level above it and states none of its own.</summary>
    bool InheritsFromParent { get; }

    /// <summary>Gets whether the level states thresholds of its own. When false and it does not inherit, it states that it has none.</summary>
    bool StatesThresholds { get; }

    /// <summary>
    /// Gets the levels the mode may be stated at, as <c>WaitThresholdOwners</c> names them: the server has no level above it, so it cannot inherit, and the console offers the
    /// server only the modes that list it.
    /// </summary>
    IReadOnlyList<string> Levels { get; }
}
