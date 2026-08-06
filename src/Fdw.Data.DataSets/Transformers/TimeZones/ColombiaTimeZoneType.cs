using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// Colombia Bogota timezone (COT, UTC-5).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Colombia")]
public sealed class ColombiaTimeZoneType : TimeZoneTypeBase
{
    public ColombiaTimeZoneType() : base(21, "Colombia", "SA Pacific Standard Time") { }
}
