using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Messages;

/// <summary>
/// CurrentMessage indicating that a service was successfully registered.
/// </summary>
[Message("ServiceRegistered")]
public sealed class ServiceRegisteredMessage : RegistrationMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceRegisteredMessage"/> class.
    /// </summary>
    public ServiceRegisteredMessage()
        : base(2200, "ServiceRegistered", MessageSeverity.Information,
               "Service registered successfully", "REG001")
    { }

    /// <summary>
    /// Initializes a new instance with the service type and lifetimeBase.
    /// </summary>
    /// <param name="serviceType">The type of service registered.</param>
    /// <param name="lifetime">The service lifetimeBase.</param>
    public ServiceRegisteredMessage(string serviceType, string lifetime)
        : base(2200, "ServiceRegistered", MessageSeverity.Information,
               $"Service '{serviceType}' registered as {lifetime}", "REG001")
    { }
}