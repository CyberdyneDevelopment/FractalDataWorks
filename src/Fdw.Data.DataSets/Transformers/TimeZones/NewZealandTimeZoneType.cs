using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// New Zealand Standard Time (NZST/NZDT, UTC+12/+13).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "NewZealand")]
public sealed class NewZealandTimeZoneType : TimeZoneTypeBase
{
    public NewZealandTimeZoneType() : base(15, "NewZealand", "New Zealand Standard Time") { }
}
