using Fdw.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;
using Fdw.Conventions;

namespace Fdw.Operations;

/// <summary>
/// Base class for operations domain service type definitions.
/// </summary>
public abstract class OperationsServiceTypeBase : ServiceTypeBase<IGenericService, IOperationsServiceFactory, IServiceConfiguration>, IOperationsServiceType
{
    /// <summary>The category a type option states none of its own sits under.</summary>
    [PlatformDefault]
    private const string DefaultCategory = "Operations";

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationsServiceTypeBase"/> class.
    /// </summary>
    protected OperationsServiceTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string? category = null)
        : base(name, sectionName, displayName, description, category ?? DefaultCategory)
    {
    }
}
