using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Etl;

/// <summary>
/// Base class for pipeline service type definitions.
/// Provides metadata and factory creation for pipeline services with typed provider support.
/// </summary>
/// <typeparam name="TPipeline">The pipeline service interface type.</typeparam>
/// <typeparam name="TFactory">The factory type for creating pipeline instances.</typeparam>
/// <typeparam name="TConfiguration">The configuration type for the pipeline.</typeparam>
/// <remarks>
/// <para>
/// This base class uses the 3-parameter ServiceTypeBase implementation.
/// Concrete types implement RegisterFactory to register with the provider.
/// The loader looks up configurations from IOptions&lt;List&lt;TConfiguration&gt;&gt; by Name.
/// </para>
/// </remarks>
public abstract class EtlPipelineTypeBase<TPipeline, TFactory, TConfiguration> :
    ServiceTypeBase<TPipeline, TFactory, TConfiguration>,
    IEtlPipelineType<TPipeline, TConfiguration, TFactory>
    where TPipeline : IEtlPipeline
    where TConfiguration : class, IGenericConfiguration
    where TFactory : IEtlPipelineFactory<TPipeline, TConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EtlPipelineTypeBase{TPipeline,TFactory,TConfiguration}"/> class.
    /// </summary>
    /// <param name="name">The name of this pipeline type.</param>
    /// <param name="sectionName">The configuration section name.</param>
    /// <param name="displayName">The display name for this pipeline type.</param>
    /// <param name="description">The description of what this pipeline type provides.</param>
    /// <param name="category">The category for this pipeline type.</param>
    /// <param name="defaultContainerName">The default container name for this pipeline type.</param>
    protected EtlPipelineTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string? category = null,
        string defaultContainerName = "")
        : base(name, sectionName, displayName, description, category ?? "Pipeline",
               defaultDataStoreName: "PlatformConfiguration",
               defaultPathName: "pipe",
               defaultContainerName: defaultContainerName)
    {
    }

    // which registers configuration loader using IOptions<List<TConfiguration>> lookup by Name

    // ── Domain-provider/domain-configuration-provider registration ─────────────────────────────
    // An overload of Registration/Register, not a new phase and not a new method name. Stored and
    // invoked exactly like the DI-wiring Registration(Func<IHostApplicationBuilder, ...>) overload
    // already inherited from ServiceTypeBase -- this is simply a second signature of the same verb,
    // distinguished by its parameter types.

    private Func<IServiceProvider, IEtlPipelineProvider?, IPipelineConfigurationProvider?, ILogger<IEtlPipelineType>, IGenericResult> _domainRegistrationMethod
        = static (_, _, _, _) => GenericResult.Success();

    /// <inheritdoc/>
    public void Registration(Func<IServiceProvider, IEtlPipelineProvider?, IPipelineConfigurationProvider?, ILogger<IEtlPipelineType>, IGenericResult> method)
    {
        _domainRegistrationMethod = method;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Base default: an option that never called the overload above reports success and leaves
    /// both providers untouched.
    /// </remarks>
    public IGenericResult Register(IServiceProvider serviceProvider, IEtlPipelineProvider? domainProvider, IPipelineConfigurationProvider? domainConfigurationProvider, ILogger<IEtlPipelineType> logger)
        => _domainRegistrationMethod(serviceProvider, domainProvider, domainConfigurationProvider, logger);
}
