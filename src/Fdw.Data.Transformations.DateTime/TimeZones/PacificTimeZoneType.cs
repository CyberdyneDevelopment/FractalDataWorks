using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>
/// US Pacific timezone.
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Pacific")]
public sealed class PacificTimeZoneType : TimeZoneTypeBase
{
    public PacificTimeZoneType() : base("Pacific", "Pacific Standard Time") { }
}
