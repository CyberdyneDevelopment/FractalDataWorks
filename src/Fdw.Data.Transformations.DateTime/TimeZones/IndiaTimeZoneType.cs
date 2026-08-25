using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>
/// India Standard Time (IST, UTC+5:30).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "India")]
public sealed class IndiaTimeZoneType : TimeZoneTypeBase
{
    public IndiaTimeZoneType() : base(11, "India", "India Standard Time") { }
}
