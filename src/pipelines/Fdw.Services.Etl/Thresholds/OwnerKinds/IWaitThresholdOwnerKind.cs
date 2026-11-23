using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Services.Etl.Abstractions.Thresholds;

namespace Fdw.Services.Etl.Thresholds;

/// <summary>A kind of owner a wait threshold level can have: the server, a pipeline or a schedule.</summary>
public interface IWaitThresholdOwnerKind : ITypeOption<int, WaitThresholdOwnerKindBase>
{
    /// <summary>Tells whether the owner a stored level names exists.</summary>
    /// <param name="owner">The owner's name.</param>
    /// <param name="pipelines">The levels of the pipelines that exist, by name.</param>
    /// <param name="schedules">The levels of the schedules that exist, by name.</param>
    /// <returns><c>true</c> when the owner exists, or when this kind has no owner to find.</returns>
    bool OwnerExists(string owner, IReadOnlyDictionary<string, WaitThresholdLayer> pipelines, IReadOnlyDictionary<string, WaitThresholdLayer> schedules);
}
