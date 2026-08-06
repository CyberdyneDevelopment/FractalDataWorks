using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// Japan Standard Time (JST, UTC+9).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Japan")]
public sealed class JapanTimeZoneType : TimeZoneTypeBase
{
    public JapanTimeZoneType() : base(13, "Japan", "Tokyo Standard Time") { }
}
