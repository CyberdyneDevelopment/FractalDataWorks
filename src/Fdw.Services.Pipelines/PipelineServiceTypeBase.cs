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

    // ── Domain-provider/domain-configuration-provider registration ─────────────────────────────
    // An overload of Registration/Register, not a new phase and not a new method name. Stored and
    // invoked exactly like the DI-wiring Registration(Func<IHostApplicationBuilder, ...>) overload
    // already inherited from ServiceTypeBase -- this is simply a second signature of the same verb,
    // distinguished by its parameter types.

    private Func<IServiceProvider, IPipelineServiceProvider?, IPipelineConfigurationProvider?, ILogger<IPipelineServiceType>, IGenericResult> _domainRegistrationMethod
        = static (_, _, _, _) => GenericResult.Success();

    /// <inheritdoc/>
    public void Registration(Func<IServiceProvider, IPipelineServiceProvider?, IPipelineConfigurationProvider?, ILogger<IPipelineServiceType>, IGenericResult> method)
    {
        _domainRegistrationMethod = method;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Base default: an option that never called the overload above reports success and leaves
    /// both providers untouched.
    /// </remarks>
    public IGenericResult Register(IServiceProvider serviceProvider, IPipelineServiceProvider? domainProvider, IPipelineConfigurationProvider? domainConfigurationProvider, ILogger<IPipelineServiceType> logger)
        => _domainRegistrationMethod(serviceProvider, domainProvider, domainConfigurationProvider, logger);
}
