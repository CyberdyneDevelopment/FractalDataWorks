using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// China Standard Time (CST, UTC+8).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "China")]
public sealed class ChinaTimeZoneType : TimeZoneTypeBase
{
    public ChinaTimeZoneType() : base(12, "China", "China Standard Time") { }
}
