using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Etl.Abstractions.Monitoring;

/// <summary>
/// Service for sending alerts and notifications.
/// </summary>
public interface IAlertingService
{
    /// <summary>
    /// Sends an alert.
    /// </summary>
    /// <param name="alert">The alert to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<IGenericResult> SendAlert(IAlert alert, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers an alert rule.
    /// </summary>
    /// <param name="rule">The alert rule.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<IGenericResult> RegisterRule(IAlertRule rule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates alert rules against current metrics.
    /// </summary>
    /// <param name="metrics">Current metrics.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of triggered alerts.</returns>
    Task<IGenericResult<IReadOnlyList<IAlert>>> EvaluateRules(
        IDictionary<string, double> metrics,
        CancellationToken cancellationToken = default);
}

// AlertSeverity enum replaced by AlertSeverities TypeCollection
// See Fdw.Etl.Monitoring.Abstractions.AlertSeverities namespace

// ComparisonOperator enum replaced by ComparisonOperators TypeCollection
// See Fdw.Etl.Monitoring.Abstractions.ComparisonOperators namespace
