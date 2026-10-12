using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>
/// Australia Central timezone (ACST/ACDT, Adelaide, UTC+9:30).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "AustraliaCentral")]
public sealed class AustraliaCentralTimeZoneType : TimeZoneTypeBase
{
    public AustraliaCentralTimeZoneType() : base("AustraliaCentral", "Cen. Australia Standard Time") { }
}
