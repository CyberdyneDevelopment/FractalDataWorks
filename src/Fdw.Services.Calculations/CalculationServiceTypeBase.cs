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
        // Why: pass the category through as-is — ServiceTypeBase already accepts a nullable category.
        // No "Calculation" default: a fabricated category value is a silent fallback (NO-FALLBACKS rule).
        : base(name, sectionName, displayName, description, category)
    {
    }
}
