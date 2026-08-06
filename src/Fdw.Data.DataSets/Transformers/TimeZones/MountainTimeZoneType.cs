using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// US Mountain timezone.
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Mountain")]
public sealed class MountainTimeZoneType : TimeZoneTypeBase
{
    public MountainTimeZoneType() : base(5, "Mountain", "Mountain Standard Time") { }
}
