using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>
/// Greenwich Mean Time (equivalent to UTC for most purposes).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "GMT")]
public sealed class GmtTimeZoneType : TimeZoneTypeBase
{
    public GmtTimeZoneType() : base("GMT", "GMT Standard Time") { }
}
