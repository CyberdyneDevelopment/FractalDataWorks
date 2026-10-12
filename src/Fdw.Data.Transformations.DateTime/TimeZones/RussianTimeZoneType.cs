using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>
/// Russian Moscow timezone (MSK, UTC+3).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Russian")]
public sealed class RussianTimeZoneType : TimeZoneTypeBase
{
    public RussianTimeZoneType() : base("Russian", "Russian Standard Time") { }
}
