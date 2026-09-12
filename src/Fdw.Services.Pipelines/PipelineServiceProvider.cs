using Fdw.Abstractions;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines;

/// <summary>
/// The Pipelines domain provider. Resolves a pipeline service by the row's name or id, dispatching to
/// the factory registered for the row's <c>Implementation</c>.
/// </summary>
/// <remarks>
/// Exists so the domain's own <see cref="IPipelineServiceProvider"/> has a concrete type behind it;
/// the behaviour is entirely the base's. Closed over <see cref="IPipelineConfigurationProvider"/>
/// rather than the raw <c>IImplementationConfigurationProvider&lt;&gt;</c>, because the base's factory
/// registries are static per closed generic — a second closing of the same domain gets its own empty
/// registry and silently builds nothing.
/// </remarks>
public sealed class PipelineServiceProvider
    : DomainServiceProviderBase<
          IGenericService,
          IPipelineImplementationConfiguration,
          IServiceFactory<IGenericService>,
          IPipelineConfigurationProvider>,
      IPipelineServiceProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineServiceProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public PipelineServiceProvider(ILogger<PipelineServiceProvider> logger)
        : base(logger)
    {
    }
}
