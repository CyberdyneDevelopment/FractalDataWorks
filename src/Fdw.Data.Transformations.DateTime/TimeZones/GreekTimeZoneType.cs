using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>
/// Greek timezone (EET/EEST, Athens, Bucharest, UTC+2).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Greek")]
public sealed class GreekTimeZoneType : TimeZoneTypeBase
{
    public GreekTimeZoneType() : base("Greek", "GTB Standard Time") { }
}
