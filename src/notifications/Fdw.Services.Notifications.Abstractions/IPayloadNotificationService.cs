using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// A notification service that knows payload types (FDW-6138): for a payload of a type it handles it receives the payload itself; for any other it is sent the words as every service is.
/// </summary>
public interface IPayloadNotificationService : INotificationService
{
    /// <summary>Says whether the service handles payloads of a type.</summary>
    /// <param name="payloadType">The name of the payload type.</param>
    /// <returns><see langword="true"/> when it receives such a payload through <see cref="SendPayload"/>.</returns>
    bool Handles(string payloadType);

    /// <summary>Sends a request that carries a payload of a type the service handles.</summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">Cancels the send.</param>
    /// <returns>The result of the send.</returns>
    Task<IGenericResult<INotificationResult>> SendPayload(IPayloadNotificationRequest request, CancellationToken cancellationToken = default);
}
