using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Etl.Abstractions.Monitoring.AlertSeverityOptions;

/// <summary>
/// Critical alert severity.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AlertSeverities), "Critical", RestrictToCurrentCompilation = true)]
public sealed class CriticalSeverity : AlertSeverityBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CriticalSeverity"/> class.
    /// </summary>
    public CriticalSeverity() : base(3, "Critical", severityLevel: 3, requiresImmediateAction: true) { }
}
