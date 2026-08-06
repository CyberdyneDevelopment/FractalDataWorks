using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// Mexico Central timezone (CST, UTC-6).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Mexico")]
public sealed class MexicoTimeZoneType : TimeZoneTypeBase
{
    public MexicoTimeZoneType() : base(22, "Mexico", "Central Standard Time (Mexico)") { }
}
