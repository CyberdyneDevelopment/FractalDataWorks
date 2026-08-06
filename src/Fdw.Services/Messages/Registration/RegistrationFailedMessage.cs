using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Messages;

/// <summary>
/// CurrentMessage indicating that service registration failed.
/// </summary>
[Message("RegistrationFailed")]
public sealed class RegistrationFailedMessage : RegistrationMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegistrationFailedMessage"/> class.
    /// </summary>
    public RegistrationFailedMessage()
        : base(2201, "RegistrationFailed", MessageSeverity.Error,
               "Service registration failed", "REG002")
    { }

    /// <summary>
    /// Initializes a new instance with the service type and failure reason.
    /// </summary>
    /// <param name="serviceType">The type of service that failed to register.</param>
    /// <param name="reason">The reason for the failure.</param>
    public RegistrationFailedMessage(string serviceType, string reason)
        : base(2201, "RegistrationFailed", MessageSeverity.Error,
               $"Failed to register service '{serviceType}': {reason}", "REG002")
    { }
}