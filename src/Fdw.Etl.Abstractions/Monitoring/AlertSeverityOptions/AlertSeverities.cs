using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Etl.Abstractions.Monitoring.AlertSeverityOptions;

/// <summary>
/// TypeCollection for alert severity levels.
/// </summary>
/// <remarks>
/// Provides compile-time discovery and O(1) lookup for alert severities.
/// Source generator creates static properties for each registered alert severity.
/// </remarks>
[TypeCollection(typeof(AlertSeverityBase), typeof(IAlertSeverity), typeof(AlertSeverities))]
public sealed partial class AlertSeverities : TypeCollectionBase<AlertSeverityBase, IAlertSeverity>
{
}
