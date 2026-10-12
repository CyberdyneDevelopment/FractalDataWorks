using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Etl.Abstractions.Monitoring.AlertSeverityOptions;

/// <summary>
/// Informational alert severity.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AlertSeverities), "Info", RestrictToCurrentCompilation = true)]
public sealed class InfoSeverity : AlertSeverityBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InfoSeverity"/> class.
    /// </summary>
    public InfoSeverity() : base(0, "Info", severityLevel: 0, requiresImmediateAction: false) { }
}
