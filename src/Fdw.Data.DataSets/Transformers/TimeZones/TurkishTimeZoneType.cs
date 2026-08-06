using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// Turkey timezone (TRT, UTC+3).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Turkish")]
public sealed class TurkishTimeZoneType : TimeZoneTypeBase
{
    public TurkishTimeZoneType() : base(27, "Turkish", "Turkey Standard Time") { }
}
