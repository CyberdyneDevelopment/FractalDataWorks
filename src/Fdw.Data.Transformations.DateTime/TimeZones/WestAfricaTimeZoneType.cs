using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>
/// West Central Africa timezone (WAT, UTC+1).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "WestAfrica")]
public sealed class WestAfricaTimeZoneType : TimeZoneTypeBase
{
    public WestAfricaTimeZoneType() : base("WestAfrica", "W. Central Africa Standard Time") { }
}
