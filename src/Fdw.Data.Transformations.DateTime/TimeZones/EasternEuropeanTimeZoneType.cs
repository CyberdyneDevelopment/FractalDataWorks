using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>
/// Eastern European Time (EET/EEST).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "EasternEuropean")]
public sealed class EasternEuropeanTimeZoneType : TimeZoneTypeBase
{
    public EasternEuropeanTimeZoneType() : base("EasternEuropean", "E. Europe Standard Time") { }
}
