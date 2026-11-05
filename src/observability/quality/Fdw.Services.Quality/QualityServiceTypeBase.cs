using Fdw.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;
using Fdw.Conventions;

namespace Fdw.Services.Quality;

/// <summary>
/// Base class for quality domain service type definitions.
/// </summary>
public abstract class QualityServiceTypeBase : ServiceTypeBase<IGenericService, IQualityServiceFactory, IServiceConfiguration>, IQualityServiceType
{
    /// <summary>The category this kind of type is listed under when a type states none of its own.</summary>
    [PlatformDefault]
    public const string DefaultCategory = "Quality";

    /// <summary>
    /// Initializes a new instance of the <see cref="QualityServiceTypeBase"/> class.
    /// </summary>
    /// <param name="name">The name of the quality service type.</param>
    /// <param name="sectionName">The configuration section name.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="description">The description.</param>
    /// <param name="category">The optional category.</param>
    protected QualityServiceTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string? category = null)
        : base(name, sectionName, displayName, description, category ?? DefaultCategory)
    {
    }
}
