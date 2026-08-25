using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>
/// Australia Western timezone (AWST, Perth, UTC+8).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "AustraliaWestern")]
public sealed class AustraliaWesternTimeZoneType : TimeZoneTypeBase
{
    public AustraliaWesternTimeZoneType() : base(41, "AustraliaWestern", "W. Australia Standard Time") { }
}
