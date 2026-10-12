using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>
/// Mexico Central timezone (CST, UTC-6).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Mexico")]
public sealed class MexicoTimeZoneType : TimeZoneTypeBase
{
    public MexicoTimeZoneType() : base("Mexico", "Central Standard Time (Mexico)") { }
}
