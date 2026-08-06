using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// Venezuela timezone (VET, UTC-4).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Venezuela")]
public sealed class VenezuelaTimeZoneType : TimeZoneTypeBase
{
    public VenezuelaTimeZoneType() : base(20, "Venezuela", "Venezuela Standard Time") { }
}
