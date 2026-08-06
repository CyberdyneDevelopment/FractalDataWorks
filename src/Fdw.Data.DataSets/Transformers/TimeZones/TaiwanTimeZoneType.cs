using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// Taiwan timezone (CST, Taipei, UTC+8).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Taiwan")]
public sealed class TaiwanTimeZoneType : TimeZoneTypeBase
{
    public TaiwanTimeZoneType() : base(39, "Taiwan", "Taipei Standard Time") { }
}
