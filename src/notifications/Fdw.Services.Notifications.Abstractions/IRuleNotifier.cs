using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Sends a notification through a named notification rule. The rule's own conditions (enabled, scope) decide whether
/// it sends; a rule that declines is a success that sent nothing.
/// </summary>
public interface IRuleNotifier
{
    /// <summary>
    /// Confirms that a notification rule of the given name is configured.
    /// </summary>
    /// <param name="ruleName">The notification rule name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success when the rule exists; a RuleNotFound failure when it does not.</returns>
    Task<IGenericResult> Exists(string ruleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification through its named rule.
    /// </summary>
    /// <param name="notification">The notification to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success when the notification was sent or the rule declined it; a failure when the rule, its channel or its severity is unusable or the send failed.</returns>
    Task<IGenericResult> Notify(RuleNotification notification, CancellationToken cancellationToken = default);
}
