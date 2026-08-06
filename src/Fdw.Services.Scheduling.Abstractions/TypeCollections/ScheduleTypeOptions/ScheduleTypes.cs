using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Scheduling.Abstractions.TypeCollections.ScheduleTypeOptions;

/// <summary>
/// TypeCollection for schedule types.
/// Source generator will populate with all discovered TypeOptions.
/// </summary>
[TypeCollection(typeof(ScheduleTypeBase), typeof(IScheduleType), typeof(ScheduleTypes))]
public sealed partial class ScheduleTypes : TypeCollectionBase<ScheduleTypeBase, IScheduleType>
{
}
