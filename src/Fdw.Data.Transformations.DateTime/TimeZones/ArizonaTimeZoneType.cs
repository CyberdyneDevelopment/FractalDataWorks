using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>
/// US Arizona timezone (MST, UTC-7, no daylight saving).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Arizona")]
public sealed class ArizonaTimeZoneType : TimeZoneTypeBase
{
    public ArizonaTimeZoneType() : base(23, "Arizona", "US Mountain Standard Time") { }
}
