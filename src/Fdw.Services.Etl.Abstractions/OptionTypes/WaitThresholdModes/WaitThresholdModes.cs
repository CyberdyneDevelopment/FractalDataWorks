using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Whether a pipeline or a schedule states thresholds on waiting runs of its own, states none, or takes those of the level above it: Inherit, Own or None (FDW-3161).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(WaitThresholdModeBase), typeof(IWaitThresholdMode), typeof(WaitThresholdModes))]
public abstract partial class WaitThresholdModes : TypeCollectionBase<WaitThresholdModeBase, IWaitThresholdMode>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
