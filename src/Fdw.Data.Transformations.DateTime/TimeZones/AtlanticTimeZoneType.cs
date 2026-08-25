using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>
/// Canada Atlantic timezone (AST/ADT, UTC-4).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Atlantic")]
public sealed class AtlanticTimeZoneType : TimeZoneTypeBase
{
    public AtlanticTimeZoneType() : base(16, "Atlantic", "Atlantic Standard Time") { }
}
