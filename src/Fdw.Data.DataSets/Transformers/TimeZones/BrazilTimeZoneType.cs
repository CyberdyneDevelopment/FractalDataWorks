using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// Brazil Brasilia timezone (BRT/BRST, UTC-3).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Brazil")]
public sealed class BrazilTimeZoneType : TimeZoneTypeBase
{
    public BrazilTimeZoneType() : base(19, "Brazil", "E. South America Standard Time") { }
}
