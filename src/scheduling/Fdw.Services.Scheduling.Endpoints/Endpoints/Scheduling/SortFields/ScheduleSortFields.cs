using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>
/// The columns a schedule list can be sorted by.
/// </summary>
/// <remarks>
/// A request names a column by its lower-case word, matched exactly through <c>ByKey</c>; a word that matches none gets
/// <c>NotFound</c> and the list refuses the request, never sorting by another column. Each option states its own ordering.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ScheduleSortFieldBase), typeof(IScheduleSortField), typeof(ScheduleSortFields))]
public abstract partial class ScheduleSortFields : TypeCollectionBase<ScheduleSortFieldBase, IScheduleSortField>
{
}
