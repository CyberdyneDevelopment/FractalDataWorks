using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>
/// Arabian timezone (GST, Dubai, Muscat, UTC+4).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Arabian")]
public sealed class ArabianTimeZoneType : TimeZoneTypeBase
{
    public ArabianTimeZoneType() : base("Arabian", "Arabian Standard Time") { }
}
