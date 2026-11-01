using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>
/// New Zealand Standard Time (NZST/NZDT, UTC+12/+13).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "NewZealand")]
public sealed class NewZealandTimeZoneType : TimeZoneTypeBase
{
    public NewZealandTimeZoneType() : base("NewZealand", "New Zealand Standard Time") { }
}
