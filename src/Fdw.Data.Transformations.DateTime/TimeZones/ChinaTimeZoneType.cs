using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>
/// China Standard Time (CST, UTC+8).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "China")]
public sealed class ChinaTimeZoneType : TimeZoneTypeBase
{
    public ChinaTimeZoneType() : base("China", "China Standard Time") { }
}
