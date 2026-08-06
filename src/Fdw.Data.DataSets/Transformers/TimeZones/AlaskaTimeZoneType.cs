using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// US Alaska timezone.
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Alaska")]
public sealed class AlaskaTimeZoneType : TimeZoneTypeBase
{
    public AlaskaTimeZoneType() : base(6, "Alaska", "Alaskan Standard Time") { }
}
