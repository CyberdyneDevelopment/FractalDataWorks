using Fdw.Collections;

namespace Fdw.Etl.Abstractions.Monitoring.AlertSeverityOptions;

/// <summary>
/// Interface for alert severity levels.
/// Extends ITypeOption to enable TypeCollection discovery.
/// </summary>
public interface IAlertSeverity : ITypeOption<int, AlertSeverityBase>
{
    /// <summary>
    /// Gets the numeric severity level (higher = more severe).
    /// </summary>
    int SeverityLevel { get; }

    /// <summary>
    /// Gets a value indicating whether this severity requires immediate action.
    /// </summary>
    bool RequiresImmediateAction { get; }
}
