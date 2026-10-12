using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>
/// Central European Time (CET/CEST).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "CentralEuropean")]
public sealed class CentralEuropeanTimeZoneType : TimeZoneTypeBase
{
    public CentralEuropeanTimeZoneType() : base("CentralEuropean", "Central European Standard Time") { }
}
