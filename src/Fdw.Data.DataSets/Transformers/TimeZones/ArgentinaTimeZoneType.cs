using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// Argentina timezone (ART, UTC-3).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Argentina")]
public sealed class ArgentinaTimeZoneType : TimeZoneTypeBase
{
    public ArgentinaTimeZoneType() : base(18, "Argentina", "Argentina Standard Time") { }
}
