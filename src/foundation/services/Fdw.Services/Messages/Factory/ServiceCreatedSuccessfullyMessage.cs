using System.Globalization;
using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Messages;

/// <summary>
/// CurrentMessage indicating that service was created successfully.
/// The source generator will create FactoryMessages.ServiceCreatedSuccessfully(serviceType) method.
/// </summary>
[Message("ServiceCreatedSuccessfullyMessage")]
public sealed class ServiceCreatedSuccessfullyMessage : FactoryMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceCreatedSuccessfullyMessage"/> class with default message template.
    /// </summary>
    public ServiceCreatedSuccessfullyMessage()
        : base(2010, "ServiceCreatedSuccessfully", MessageSeverity.Information,
               "Successfully created service of type {0}", "SERVICE_CREATED_SUCCESSFULLY")
    { }

    /// <summary>
    /// Initializes a new instance with the service type that was created successfully.
    /// </summary>
    /// <param name="serviceType">The type of service that was created successfully.</param>
    public ServiceCreatedSuccessfullyMessage(string serviceType)
        : base(2010, "ServiceCreatedSuccessfully", MessageSeverity.Information,
               string.Format(CultureInfo.InvariantCulture, "Successfully created service of type {0}", serviceType),
               "SERVICE_CREATED_SUCCESSFULLY")
    { }

}