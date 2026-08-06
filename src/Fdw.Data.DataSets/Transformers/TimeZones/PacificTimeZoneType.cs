using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// US Pacific timezone.
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Pacific")]
public sealed class PacificTimeZoneType : TimeZoneTypeBase
{
    public PacificTimeZoneType() : base(4, "Pacific", "Pacific Standard Time") { }
}
