using Fdw.Etl.Abstractions.Monitoring.AlertSeverityOptions;
using Fdw.Etl.Abstractions.Monitoring.ComparisonOperatorOptions;

namespace Fdw.Etl.Abstractions.Monitoring;

/// <summary>
/// Represents an alert rule.
/// </summary>
public interface IAlertRule
{
    /// <summary>
    /// Gets the rule ID.
    /// </summary>
    string RuleId { get; }

    /// <summary>
    /// Gets the rule name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the metric to monitor.
    /// </summary>
    string MetricName { get; }

    /// <summary>
    /// Gets the comparison operator.
    /// </summary>
    IComparisonOperator Operator { get; }

    /// <summary>
    /// Gets the threshold value.
    /// </summary>
    double Threshold { get; }

    /// <summary>
    /// Gets the alert severity.
    /// </summary>
    IAlertSeverity Severity { get; }

    /// <summary>
    /// Gets whether the rule is enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Evaluates the rule against a metric value.
    /// </summary>
    /// <param name="value">The metric value.</param>
    /// <returns>True if the rule is triggered.</returns>
    bool Evaluate(double value);
}