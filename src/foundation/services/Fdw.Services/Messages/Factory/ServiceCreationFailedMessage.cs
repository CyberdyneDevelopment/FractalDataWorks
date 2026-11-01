using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Messages;

/// <summary>
/// CurrentMessage indicating that service creation failed.
/// The source generator will create FactoryMessages.ServiceCreationFailed(serviceType) method.
/// </summary>
[Message("ServiceCreationFailed")]
[ExcludeFromCodeCoverage]
public sealed class ServiceCreationFailedMessage : FactoryMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceCreationFailedMessage"/> class with default message template.
    /// </summary>
    public ServiceCreationFailedMessage()
        : base(2001, "ServiceCreationFailed", MessageSeverity.Error,
               "Failed to create service of type {0}", "SERVICE_CREATION_FAILED")
    { }

    /// <summary>
    /// Initializes a new instance with the service type that failed creation.
    /// </summary>
    /// <param name="serviceType">The type of service that failed to be created.</param>
    public ServiceCreationFailedMessage(string serviceType)
        : base(2001, "ServiceCreationFailed", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "Failed to create service of type {0}", serviceType),
               "SERVICE_CREATION_FAILED")
    { }

    /// <summary>
    /// Initializes a new instance with the service type and failure reason.
    /// </summary>
    /// <param name="serviceType">The type of service that failed to be created.</param>
    /// <param name="reason">The reason for the failure.</param>
    public ServiceCreationFailedMessage(string serviceType, string reason)
        : base(2001, "ServiceCreationFailed", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "Failed to create service of type {0}: {1}", serviceType, reason),
               "SERVICE_CREATION_FAILED")
    { }
}