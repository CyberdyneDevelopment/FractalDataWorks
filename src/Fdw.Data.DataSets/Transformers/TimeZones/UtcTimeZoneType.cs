using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// UTC timezone.
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "UTC")]
public sealed class UtcTimeZoneType : TimeZoneTypeBase
{
    public UtcTimeZoneType() : base(1, "UTC", "UTC") { }
}
