using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>
/// Fiji timezone (FJT/FJST, UTC+12).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Fiji")]
public sealed class FijiTimeZoneType : TimeZoneTypeBase
{
    public FijiTimeZoneType() : base(42, "Fiji", "Fiji Standard Time") { }
}
