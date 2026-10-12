namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// The notification a Notify stage states, as a request carries it.
/// </summary>
public interface ITransformNotifyingSpec
{
    /// <summary>Gets the notification the stage states; null when the request states none.</summary>
    INotificationSpec? Notification { get; }
}
