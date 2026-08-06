using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// Russian Moscow timezone (MSK, UTC+3).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Russian")]
public sealed class RussianTimeZoneType : TimeZoneTypeBase
{
    public RussianTimeZoneType() : base(26, "Russian", "Russian Standard Time") { }
}
