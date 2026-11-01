using FastEndpoints;
using Fdw.Services.Pipelines;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// Endpoint to get the dataflow graph every <c>pipelines:read</c> caller may see: the pipelines, the
/// data sets they reach, and the datastores and containers those are stored in.
/// </summary>
public abstract class GetDataflowGraphEndpointBase : DataflowLayerEndpointBase
{
    /// <inheritdoc />
    protected GetDataflowGraphEndpointBase(
        DataflowGraphConfigurationProvider provider,
        PipelineServiceConfigurationProvider pipelineProvider,
        ILogger<GetDataflowGraphEndpointBase> logger)
        : base(provider, pipelineProvider, logger)
    {
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/dataflow/graph");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:read");
#endif
        Summary(s =>
        {
            s.Summary = "Get dataflow graph";
            s.Description = "Returns the dataflow graph reachable from the configured pipelines: pipelines, the DataSets they read and write, and the DataStores and containers those are stored in. Sources and connections are served by /dataflow/graph/sources.";
        });
    }

    private protected override bool NamesCategories => true;

    private protected override DataflowGraphResponse BuildLayer(DataflowGraphInputs inputs)
        => DataflowBaseLayer.Build(inputs);
}
