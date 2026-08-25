using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>
/// Western European timezone (CET/CEST, Amsterdam, Berlin, UTC+1).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "WesternEuropean")]
public sealed class WesternEuropeanTimeZoneType : TimeZoneTypeBase
{
    public WesternEuropeanTimeZoneType() : base(24, "WesternEuropean", "W. Europe Standard Time") { }
}
