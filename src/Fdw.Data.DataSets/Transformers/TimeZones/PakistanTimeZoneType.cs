using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// Pakistan timezone (PKT, UTC+5).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Pakistan")]
public sealed class PakistanTimeZoneType : TimeZoneTypeBase
{
    public PakistanTimeZoneType() : base(34, "Pakistan", "Pakistan Standard Time") { }
}
