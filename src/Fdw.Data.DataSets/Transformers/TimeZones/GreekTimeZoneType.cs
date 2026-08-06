using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// Greek timezone (EET/EEST, Athens, Bucharest, UTC+2).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Greek")]
public sealed class GreekTimeZoneType : TimeZoneTypeBase
{
    public GreekTimeZoneType() : base(28, "Greek", "GTB Standard Time") { }
}
