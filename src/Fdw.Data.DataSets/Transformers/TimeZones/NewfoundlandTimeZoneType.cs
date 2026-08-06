using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// Newfoundland timezone (NST/NDT, UTC-3:30).
/// </summary>
[TypeOption(typeof(TimeZoneTypes), "Newfoundland")]
public sealed class NewfoundlandTimeZoneType : TimeZoneTypeBase
{
    public NewfoundlandTimeZoneType() : base(17, "Newfoundland", "Newfoundland Standard Time") { }
}
