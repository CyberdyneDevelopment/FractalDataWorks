using Fdw.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;

namespace Fdw.Services.Resiliency;

/// <summary>
/// Base class for resiliency service type definitions.
/// </summary>
public abstract class ResiliencyServiceTypeBase :
    ServiceTypeBase<IGenericService, IResiliencyFactory, IServiceConfiguration>,
    IResiliencyServiceType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResiliencyServiceTypeBase"/> class.
    /// </summary>
    /// <param name="name">The name of the resiliency service type.</param>
    /// <param name="sectionName">The configuration section name.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="description">The description.</param>
    /// <param name="category">The optional category.</param>
    /// <param name="defaultContainerName">The default container name for this resiliency type.</param>
    protected ResiliencyServiceTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string? category = null,
        string defaultContainerName = "")
        : base(name, sectionName, displayName, description, category ?? "Resiliency",
               defaultDataStoreName: "PlatformConfiguration",
               defaultPathName: "conn",
               defaultContainerName: defaultContainerName)
    {
    }
}
