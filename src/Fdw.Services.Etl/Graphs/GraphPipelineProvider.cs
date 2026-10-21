using Fdw.Services.Pipelines.Abstractions;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.ServiceTypes.Logging;
using Fdw.ServiceTypes;
using Fdw.Services.Configuration;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Graphs.Commands;
using Fdw.Services.Etl;
using Fdw.Services.Pipelines;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// Builds the GraphPipeline implementation.
/// </summary>
/// <remarks>
/// Nothing to resolve here, so it completes synchronously. The provider contract is asynchronous
/// because RESOLVING may be; an implementation with nothing to fetch has nothing to await.
/// </remarks>
public sealed class GraphPipelineProvider
    : ImplementationServiceProviderBase<IEtlPipeline, IPipelineImplementationConfiguration>,
      IGraphPipelineProvider
{
    private readonly IGraphPipelineFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphPipelineProvider"/> class.
    /// </summary>
    /// <param name="factory">Builds the service.</param>
    public GraphPipelineProvider(IGraphPipelineFactory factory) => _factory = factory;

    /// <inheritdoc />
    public override Task<IGenericResult<IEtlPipeline>> Create(
        IPipelineImplementationConfiguration configuration,
        CancellationToken cancellationToken = default)
        => Task.FromResult(_factory.Create(configuration));
}
