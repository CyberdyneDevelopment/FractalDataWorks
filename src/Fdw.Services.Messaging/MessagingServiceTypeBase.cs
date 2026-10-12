using Fdw.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;

namespace Fdw.Services.Messaging;

/// <summary>
/// Base class for messaging service type definitions.
/// </summary>
public abstract class MessagingServiceTypeBase :
    ServiceTypeBase<IGenericService, IMessagingFactory, IServiceConfiguration>,
    IMessagingServiceType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessagingServiceTypeBase"/> class.
    /// </summary>
    /// <param name="name">The name of the messaging service type.</param>
    /// <param name="sectionName">The configuration section name.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="description">The description.</param>
    /// <param name="category">The optional category.</param>
    protected MessagingServiceTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string? category = null)
        : base(name, sectionName, displayName, description, category ?? "Messaging")
    {
    }
}
