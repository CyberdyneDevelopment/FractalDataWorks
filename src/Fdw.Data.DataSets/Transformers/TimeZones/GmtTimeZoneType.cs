using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// Greenwich Mean Time (equivalent to UTC for most purposes).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "GMT")]
public sealed class GmtTimeZoneType : TimeZoneTypeBase
{
    public GmtTimeZoneType() : base(8, "GMT", "GMT Standard Time") { }
}
