using Fdw.Abstractions;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines;

/// <summary>
/// The Pipelines domain provider. Resolves a pipeline service by the row's name or id, dispatching to
/// the factory registered for the row's <c>ServiceOptionType</c>.
/// </summary>
/// <remarks>
/// Exists so the domain's own <see cref="IPipelineServiceProvider"/> has a concrete type behind it;
/// the behaviour is entirely the base's. Closed over <see cref="IPipelineConfigurationProvider"/>
/// rather than the raw <c>IDomainConfigurationProvider&lt;&gt;</c>, because the base's factory
/// registries are static per closed generic — a second closing of the same domain gets its own empty
/// registry and silently builds nothing.
/// </remarks>
public sealed class PipelineServiceProvider
    : PlatformServiceProviderBase<
          IGenericService,
          IPipelineImplementationConfiguration,
          IServiceFactory<IGenericService>,
          IPipelineConfigurationProvider>,
      IPipelineServiceProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineServiceProvider"/> class.
    /// </summary>
    /// <param name="services">The container this provider resolves factories from.</param>
    /// <param name="logger">The logger instance.</param>
    public PipelineServiceProvider(IServiceProvider services, ILogger<PipelineServiceProvider> logger)
        : base(services, logger)
    {
    }
}
