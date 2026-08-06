using System.Globalization;
using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Messages;

/// <summary>
/// CurrentMessage indicating that a service type is unknown or not registered.
/// </summary>
[Message("ServiceTypeUnknown")]
public sealed class ServiceTypeUnknownMessage : ServiceMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceTypeUnknownMessage"/> class with default message template.
    /// </summary>
    public ServiceTypeUnknownMessage()
        : base(1006, "ServiceTypeUnknown", MessageSeverity.Error,
               "Unknown service type: {0}", "SERVICE_TYPE_UNKNOWN")
    { }

    /// <summary>
    /// Initializes a new instance with the unknown service type.
    /// </summary>
    /// <param name="serviceType">The service type that is unknown.</param>
    public ServiceTypeUnknownMessage(string serviceType)
        : base(1006, "ServiceTypeUnknown", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "Unknown service type: {0}", serviceType),
               "SERVICE_TYPE_UNKNOWN")
    { }

    /// <summary>
    /// Initializes a new instance with the unknown service type and additional context.
    /// </summary>
    /// <param name="serviceType">The service type that is unknown.</param>
    /// <param name="context">Additional context about where the unknown type was referenced.</param>
    public ServiceTypeUnknownMessage(string serviceType, string context)
        : base(1006, "ServiceTypeUnknown", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "Unknown service type '{0}' in {1}", serviceType, context),
               "SERVICE_TYPE_UNKNOWN")
    { }
}