using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// Singapore timezone (SGT, UTC+8).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Singapore")]
public sealed class SingaporeTimeZoneType : TimeZoneTypeBase
{
    public SingaporeTimeZoneType() : base(37, "Singapore", "Singapore Standard Time") { }
}
