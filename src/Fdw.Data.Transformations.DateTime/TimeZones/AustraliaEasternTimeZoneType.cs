using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>
/// Australian Eastern Standard Time (AEST/AEDT, UTC+10/+11).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "AustraliaEastern")]
public sealed class AustraliaEasternTimeZoneType : TimeZoneTypeBase
{
    public AustraliaEasternTimeZoneType() : base(14, "AustraliaEastern", "AUS Eastern Standard Time") { }
}
