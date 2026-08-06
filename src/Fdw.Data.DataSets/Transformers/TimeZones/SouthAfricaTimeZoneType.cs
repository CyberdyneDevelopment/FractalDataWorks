using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// South Africa timezone (SAST, UTC+2).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "SouthAfrica")]
public sealed class SouthAfricaTimeZoneType : TimeZoneTypeBase
{
    public SouthAfricaTimeZoneType() : base(29, "SouthAfrica", "South Africa Standard Time") { }
}
