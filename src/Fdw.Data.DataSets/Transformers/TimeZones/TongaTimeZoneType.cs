using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// Tonga timezone (TOT, UTC+13).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Tonga")]
public sealed class TongaTimeZoneType : TimeZoneTypeBase
{
    public TongaTimeZoneType() : base(44, "Tonga", "Tonga Standard Time") { }
}
