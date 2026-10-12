using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Etl.Abstractions.Monitoring.AlertSeverityOptions;

/// <summary>
/// Base class for alert severity levels.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption base class - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
public abstract class AlertSeverityBase : TypeOptionBase<int, AlertSeverityBase>, IAlertSeverity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AlertSeverityBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this alert severity.</param>
    /// <param name="name">The name of this alert severity.</param>
    /// <param name="severityLevel">The numeric severity level.</param>
    /// <param name="requiresImmediateAction">Whether this severity requires immediate action.</param>
    protected AlertSeverityBase(int id, string name, int severityLevel, bool requiresImmediateAction)
        : base(id, name)
    {
        SeverityLevel = severityLevel;
        RequiresImmediateAction = requiresImmediateAction;
    }

    /// <inheritdoc />
    public int SeverityLevel { get; }

    /// <inheritdoc />
    public bool RequiresImmediateAction { get; }
}
