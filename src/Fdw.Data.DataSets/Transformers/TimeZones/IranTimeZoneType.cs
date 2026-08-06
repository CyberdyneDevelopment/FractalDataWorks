using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// Iran timezone (IRST/IRDT, UTC+3:30).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Iran")]
public sealed class IranTimeZoneType : TimeZoneTypeBase
{
    public IranTimeZoneType() : base(33, "Iran", "Iran Standard Time") { }
}
