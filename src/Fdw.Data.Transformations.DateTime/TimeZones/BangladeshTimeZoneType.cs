using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>
/// Bangladesh timezone (BST, UTC+6).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Bangladesh")]
public sealed class BangladeshTimeZoneType : TimeZoneTypeBase
{
    public BangladeshTimeZoneType() : base(35, "Bangladesh", "Bangladesh Standard Time") { }
}
