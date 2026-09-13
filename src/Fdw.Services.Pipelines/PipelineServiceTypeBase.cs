using System;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Logging;

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

    /// <inheritdoc/>
    /// <remarks>
    /// Base no-op: an option with nothing to contribute reports success and leaves the domain
    /// provider's registry unchanged.
    /// </remarks>
    public virtual IGenericResult RegisterImplementationProvider(IPipelineServiceProvider domainProvider, IServiceProvider serviceProvider, ILogger logger)
        => GenericResult.Success();
}
