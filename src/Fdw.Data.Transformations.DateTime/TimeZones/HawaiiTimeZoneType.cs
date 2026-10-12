using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>
/// US Hawaii timezone (no DST).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Hawaii")]
public sealed class HawaiiTimeZoneType : TimeZoneTypeBase
{
    public HawaiiTimeZoneType() : base("Hawaii", "Hawaiian Standard Time") { }
}
