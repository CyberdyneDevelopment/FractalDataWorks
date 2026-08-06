using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// Azores timezone (AZOT/AZOST, UTC-1 with DST).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Azores")]
public sealed class AzoresTimeZoneType : TimeZoneTypeBase
{
    public AzoresTimeZoneType() : base(46, "Azores", "Azores Standard Time") { }
}
