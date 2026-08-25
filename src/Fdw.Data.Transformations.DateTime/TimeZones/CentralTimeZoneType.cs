using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>
/// US Central timezone.
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Central")]
public sealed class CentralTimeZoneType : TimeZoneTypeBase
{
    public CentralTimeZoneType() : base(2, "Central", "Central Standard Time") { }
}
