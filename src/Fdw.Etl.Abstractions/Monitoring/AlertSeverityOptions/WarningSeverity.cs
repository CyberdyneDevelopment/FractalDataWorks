using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Etl.Abstractions.Monitoring.AlertSeverityOptions;

/// <summary>
/// Warning alert severity.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AlertSeverities), "Warning", RestrictToCurrentCompilation = true)]
public sealed class WarningSeverity : AlertSeverityBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WarningSeverity"/> class.
    /// </summary>
    public WarningSeverity() : base(1, "Warning", severityLevel: 1, requiresImmediateAction: false) { }
}
