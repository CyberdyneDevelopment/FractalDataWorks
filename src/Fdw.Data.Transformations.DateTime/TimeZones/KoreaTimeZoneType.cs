using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>
/// Korea timezone (KST, Seoul, UTC+9).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Korea")]
public sealed class KoreaTimeZoneType : TimeZoneTypeBase
{
    public KoreaTimeZoneType() : base("Korea", "Korea Standard Time") { }
}
