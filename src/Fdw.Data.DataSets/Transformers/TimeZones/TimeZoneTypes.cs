using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// TypeCollection of named timezone options available to field transforms.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(TimeZoneTypeBase), typeof(ITimeZoneType), typeof(TimeZoneTypes))]
public sealed partial class TimeZoneTypes : TypeCollectionBase<TimeZoneTypeBase, ITimeZoneType>
{
}
