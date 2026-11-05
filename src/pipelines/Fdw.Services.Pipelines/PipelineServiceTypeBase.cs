using Fdw.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.ServiceTypes;
using Fdw.Conventions;

namespace Fdw.Services.Pipelines;

/// <summary>
/// Base class for pipeline-service domain service type definitions.
/// </summary>
public abstract class PipelineServiceTypeBase : ServiceTypeBase<IGenericService, IPipelineServiceFactory, IServiceConfiguration>, IPipelineServiceType
{
    /// <summary>The category a type option states none of its own sits under.</summary>
    [PlatformDefault]
    private const string DefaultCategory = "PipelineService";

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineServiceTypeBase"/> class.
    /// </summary>
    protected PipelineServiceTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string? category = null)
        : base(name, sectionName, displayName, description, category ?? DefaultCategory)
    {
    }
}
