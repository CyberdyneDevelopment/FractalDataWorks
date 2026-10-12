using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>
/// Colombia Bogota timezone (COT, UTC-5).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Colombia")]
public sealed class ColombiaTimeZoneType : TimeZoneTypeBase
{
    public ColombiaTimeZoneType() : base("Colombia", "SA Pacific Standard Time") { }
}
