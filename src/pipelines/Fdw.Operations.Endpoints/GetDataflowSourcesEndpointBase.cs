using FastEndpoints;
using Fdw.Services.Pipelines;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// Endpoint to get the sources and connections behind the dataflow graph. It requires
/// <c>connections:read</c>, so a caller without it is refused by the permission check and the
/// endpoint never inspects who is asking.
/// </summary>
public abstract class GetDataflowSourcesEndpointBase : DataflowLayerEndpointBase
{
    /// <inheritdoc />
    protected GetDataflowSourcesEndpointBase(
        DataflowGraphConfigurationProvider provider,
        PipelineServiceConfigurationProvider pipelineProvider,
        ILogger<GetDataflowSourcesEndpointBase> logger)
        : base(provider, pipelineProvider, logger)
    {
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/dataflow/graph/sources");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("connections:read");
#endif
        Summary(s =>
        {
            s.Summary = "Get dataflow graph sources and connections";
            s.Description = "Returns the data set sources, and the connections they and the pipelines use, for the same reachable set as /dataflow/graph. Its edges join nodes that endpoint returns, so a client lays this over that.";
        });
    }

    private protected override bool NamesCategories => false;

    private protected override DataflowGraphResponse BuildLayer(DataflowGraphInputs inputs)
        => DataflowSourcesLayer.Build(inputs);
}
