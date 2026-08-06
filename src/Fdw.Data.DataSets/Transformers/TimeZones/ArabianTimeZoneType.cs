using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// Arabian timezone (GST, Dubai, Muscat, UTC+4).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Arabian")]
public sealed class ArabianTimeZoneType : TimeZoneTypeBase
{
    public ArabianTimeZoneType() : base(32, "Arabian", "Arabian Standard Time") { }
}
