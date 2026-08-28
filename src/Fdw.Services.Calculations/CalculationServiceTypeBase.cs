using Fdw.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;

namespace Fdw.Services.Calculations;

/// <summary>
/// Base class for calculation domain service type definitions.
/// </summary>
public abstract class CalculationServiceTypeBase : ServiceTypeBase<IGenericService, ICalculationServiceFactory, IServiceConfiguration>, ICalculationServiceType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationServiceTypeBase"/> class.
    /// </summary>
    /// <param name="name">The name of the calculation service type.</param>
    /// <param name="sectionName">The configuration section name.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="description">The description.</param>
    /// <param name="category">The optional category.</param>
    protected CalculationServiceTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string? category = null)
        : base(name, sectionName, displayName, description, category)
    {
    }
}
