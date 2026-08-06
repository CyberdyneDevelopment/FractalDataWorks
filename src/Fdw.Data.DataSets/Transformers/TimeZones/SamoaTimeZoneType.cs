using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// Samoa timezone (WST/WSDT, UTC+13).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Samoa")]
public sealed class SamoaTimeZoneType : TimeZoneTypeBase
{
    public SamoaTimeZoneType() : base(43, "Samoa", "Samoa Standard Time") { }
}
