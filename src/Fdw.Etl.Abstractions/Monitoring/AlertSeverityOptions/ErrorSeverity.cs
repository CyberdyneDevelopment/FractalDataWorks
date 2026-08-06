using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Etl.Abstractions.Monitoring.AlertSeverityOptions;

/// <summary>
/// Error alert severity.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AlertSeverities), "Error", RestrictToCurrentCompilation = true)]
public sealed class ErrorSeverity : AlertSeverityBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorSeverity"/> class.
    /// </summary>
    public ErrorSeverity() : base(2, "Error", severityLevel: 2, requiresImmediateAction: true) { }
}
