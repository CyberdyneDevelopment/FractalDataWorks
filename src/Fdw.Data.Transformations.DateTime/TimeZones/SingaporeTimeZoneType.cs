using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>
/// Singapore timezone (SGT, UTC+8).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Singapore")]
public sealed class SingaporeTimeZoneType : TimeZoneTypeBase
{
    public SingaporeTimeZoneType() : base("Singapore", "Singapore Standard Time") { }
}
