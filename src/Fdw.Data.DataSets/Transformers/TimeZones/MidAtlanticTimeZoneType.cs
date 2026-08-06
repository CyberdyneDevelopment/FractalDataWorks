using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// Mid-Atlantic timezone (UTC-2).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "MidAtlantic")]
public sealed class MidAtlanticTimeZoneType : TimeZoneTypeBase
{
    public MidAtlanticTimeZoneType() : base(47, "MidAtlantic", "Mid-Atlantic Standard Time") { }
}
