using Fdw.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;

namespace Fdw.Operations;

/// <summary>
/// Base class for operations domain service type definitions.
/// </summary>
public abstract class OperationsServiceTypeBase : ServiceTypeBase<IGenericService, IOperationsServiceFactory, IServiceConfiguration>, IOperationsServiceType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationsServiceTypeBase"/> class.
    /// </summary>
    protected OperationsServiceTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string? category = null)
        : base(name, sectionName, displayName, description, category ?? "Operations")
    {
    }
}
