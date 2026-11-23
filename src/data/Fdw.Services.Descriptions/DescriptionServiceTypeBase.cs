using Fdw.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;

namespace Fdw.Services.Descriptions;

/// <summary>Base class for descriptions domain service type definitions.</summary>
public abstract class DescriptionServiceTypeBase : ServiceTypeBase<IGenericService, IDescriptionServiceFactory, IServiceConfiguration>, IDescriptionServiceType
{
    /// <summary>Initializes a new instance of the <see cref="DescriptionServiceTypeBase"/> class.</summary>
    /// <param name="name">The name of the descriptions service type.</param>
    /// <param name="sectionName">The configuration section name.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="description">The description.</param>
    /// <param name="category">The category the type is listed under.</param>
    protected DescriptionServiceTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string category)
        : base(name, sectionName, displayName, description, category)
    {
    }
}
