using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// What happens to a run of a pipeline triggered while as many runs of that pipeline are active as it may have at once: Queue, Refuse or DelayedStart (FDW-3117, FDW-3129).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(OverflowBehaviorBase), typeof(IOverflowBehavior), typeof(OverflowBehaviors))]
public abstract partial class OverflowBehaviors : TypeCollectionBase<OverflowBehaviorBase, IOverflowBehavior>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
