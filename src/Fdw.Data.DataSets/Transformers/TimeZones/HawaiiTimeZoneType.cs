using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// US Hawaii timezone (no DST).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Hawaii")]
public sealed class HawaiiTimeZoneType : TimeZoneTypeBase
{
    public HawaiiTimeZoneType() : base(7, "Hawaii", "Hawaiian Standard Time") { }
}
