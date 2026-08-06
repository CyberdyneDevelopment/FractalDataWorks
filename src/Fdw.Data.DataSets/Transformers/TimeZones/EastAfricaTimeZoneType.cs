using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// East Africa timezone (EAT, Nairobi, UTC+3).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "EastAfrica")]
public sealed class EastAfricaTimeZoneType : TimeZoneTypeBase
{
    public EastAfricaTimeZoneType() : base(31, "EastAfrica", "E. Africa Standard Time") { }
}
