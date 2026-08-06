using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Etl.Abstractions.Monitoring.SeverityLevelOptions;

/// <summary>
/// TypeCollection for severity levels used in telemetry traces.
/// </summary>
/// <remarks>
/// Provides compile-time discovery and O(1) lookup for severity levels.
/// Source generator creates static properties for each registered severity level.
/// </remarks>
[TypeCollection(typeof(SeverityLevelBase), typeof(ISeverityLevel), typeof(SeverityLevels))]
public sealed partial class SeverityLevels : TypeCollectionBase<SeverityLevelBase, ISeverityLevel>
{
}
