using Fdw.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;

namespace Fdw.Services.Pipelines;

/// <summary>
/// Base class for pipeline-service domain service type definitions.
/// </summary>
public abstract class PipelineServiceTypeBase : ServiceTypeBase<IGenericService, IPipelineServiceFactory, IServiceConfiguration>, IPipelineServiceType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineServiceTypeBase"/> class.
    /// </summary>
    protected PipelineServiceTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string? category = null)
        : base(name, sectionName, displayName, description, category ?? "PipelineService")
    {
    }
}
