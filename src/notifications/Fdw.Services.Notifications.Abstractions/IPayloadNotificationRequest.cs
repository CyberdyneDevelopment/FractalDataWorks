namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// A notification request that carries a payload besides its words (FDW-6138). A derived contract and never the metadata: <see cref="INotificationRequest"/> gains nothing, so no
/// implementer of it breaks, and a service that does not know payloads receives the subject and the message as it always did.
/// </summary>
public interface IPayloadNotificationRequest : INotificationRequest
{
    /// <summary>Gets the payload.</summary>
    NotificationPayload Payload { get; }
}
