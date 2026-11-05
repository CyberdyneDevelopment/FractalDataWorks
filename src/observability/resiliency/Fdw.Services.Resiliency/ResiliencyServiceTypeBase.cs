using Fdw.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;
using Fdw.Services.Data.Abstractions;
using Fdw.Conventions;

namespace Fdw.Services.Resiliency;

/// <summary>
/// Base class for resiliency service type definitions.
/// </summary>
public abstract class ResiliencyServiceTypeBase :
    ServiceTypeBase<IGenericService, IResiliencyFactory, IServiceConfiguration>,
    IResiliencyServiceType
{
    /// <summary>The category this kind of type is listed under when a type states none of its own.</summary>
    [PlatformDefault]
    public const string DefaultCategory = "Resiliency";

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
        : base(name, sectionName, displayName, description, category ?? DefaultCategory,
               defaultDataStoreName: () => PlatformConnection.Name,
               defaultPathName: "conn",
               defaultContainerName: defaultContainerName)
    {
    }
}
