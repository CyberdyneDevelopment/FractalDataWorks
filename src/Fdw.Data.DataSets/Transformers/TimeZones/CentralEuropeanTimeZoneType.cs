using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// Central European Time (CET/CEST).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "CentralEuropean")]
public sealed class CentralEuropeanTimeZoneType : TimeZoneTypeBase
{
    public CentralEuropeanTimeZoneType() : base(9, "CentralEuropean", "Central European Standard Time") { }
}
