using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// US Eastern timezone.
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Eastern")]
public sealed class EasternTimeZoneType : TimeZoneTypeBase
{
    public EasternTimeZoneType() : base(3, "Eastern", "Eastern Standard Time") { }
}
