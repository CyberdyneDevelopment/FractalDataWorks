using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// Cape Verde timezone (CVT, UTC-1).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "CapeVerde")]
public sealed class CapeVerdeTimeZoneType : TimeZoneTypeBase
{
    public CapeVerdeTimeZoneType() : base(45, "CapeVerde", "Cape Verde Standard Time") { }
}
