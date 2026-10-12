using Fdw.Collections;
using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Scheduling.Abstractions.OptionTypes;

/// <summary>
/// Collection of trigger types for scheduling system.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(TriggerTypeBase), typeof(ITriggerType), typeof(TriggerTypes))]
public abstract partial class TriggerTypes : TypeCollectionBase<TriggerTypeBase, ITriggerType>
{
    // DO NOT IMPLEMENT BY HAND!
    // Source generator automatically creates static TriggerTypes class with:
    // - TriggerTypes.Cron (returns TriggerTypeBase)
    // - TriggerTypes.Interval (returns TriggerTypeBase)
    // - TriggerTypes.Manual (returns TriggerTypeBase)
    // - TriggerTypes.Once (returns TriggerTypeBase)
    // - TriggerTypes.All (collection of TriggerTypeBase)
    // - TriggerTypes.ById(int id) (returns TriggerTypeBase)
    // - TriggerTypes.ByName(string name) (returns TriggerTypeBase)
}