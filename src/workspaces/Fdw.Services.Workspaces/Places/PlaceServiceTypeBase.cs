using Fdw.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;

namespace Fdw.Services.Workspaces;

/// <summary>
/// Base class for the place domain's service type definitions (FDW-2538).
/// </summary>
public abstract class PlaceServiceTypeBase : ServiceTypeBase<IGenericService, IPlaceServiceFactory, IServiceConfiguration>, IPlaceServiceType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaceServiceTypeBase"/> class.
    /// </summary>
    /// <param name="name">The name of the place service type.</param>
    /// <param name="sectionName">The configuration section name.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="description">The description.</param>
    /// <param name="category">The optional category.</param>
    protected PlaceServiceTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string? category = null)
        : base(name, sectionName, displayName, description, category ?? "Places")
    {
    }
}
