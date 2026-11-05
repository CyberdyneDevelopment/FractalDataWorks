using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>Sends notification rules once, for the Notify consequence (FDW-3936).</summary>
public interface IThresholdNotifier
{
    /// <summary>Sends each named rule.</summary>
    /// <param name="rules">The notification rule names.</param>
    /// <param name="subject">The one-line subject.</param>
    /// <param name="message">The body.</param>
    /// <param name="metadata">What the channel carries with the notification.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Success when every rule sent or declined; the first failure otherwise.</returns>
    Task<IGenericResult> Notify(
        IReadOnlyList<string> rules, string subject, string message, IReadOnlyDictionary<string, object?> metadata, CancellationToken cancellationToken = default);
}
