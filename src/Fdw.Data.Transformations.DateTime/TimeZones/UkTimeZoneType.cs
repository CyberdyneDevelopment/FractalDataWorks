using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>
/// United Kingdom timezone (GMT/BST, London, UTC+0 with DST).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "UK")]
public sealed class UkTimeZoneType : TimeZoneTypeBase
{
    public UkTimeZoneType() : base(25, "UK", "GMT Standard Time") { }
}
