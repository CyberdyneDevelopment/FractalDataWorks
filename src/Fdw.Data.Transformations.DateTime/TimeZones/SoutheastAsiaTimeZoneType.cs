using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>
/// Southeast Asia timezone (ICT, Bangkok, Hanoi, UTC+7).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "SoutheastAsia")]
public sealed class SoutheastAsiaTimeZoneType : TimeZoneTypeBase
{
    public SoutheastAsiaTimeZoneType() : base(36, "SoutheastAsia", "SE Asia Standard Time") { }
}
