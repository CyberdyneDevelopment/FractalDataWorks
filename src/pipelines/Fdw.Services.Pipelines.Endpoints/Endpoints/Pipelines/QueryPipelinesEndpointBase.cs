using Fdw.Configuration;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Base endpoint for querying pipelines: <c>POST /pipelines/query</c>.
/// </summary>
/// <remarks>
/// The pipelines list, asked for by filter, order, page and field: a picker that wants only ids and names, a
/// page with its total. It answers from the pipelines domain table, so no pipeline's stages are read. The
/// policy is the list's own, <c>pipelines:read</c>. The fields it accepts are declared by the endpoint that
/// registers it (<see cref="QueryDomainEndpointBase{TDomain}.QueryableFields"/>).
/// </remarks>
public abstract class QueryPipelinesEndpointBase : QueryDomainEndpointBase<DomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="QueryPipelinesEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="pipelineProvider">Reads the page from the pipelines domain table.</param>
    protected QueryPipelinesEndpointBase(ILogger logger, PipelineServiceConfigurationProvider pipelineProvider)
        : base(logger, pipelineProvider)
    {
    }

    /// <summary>Gets the resource name used for the route and the default policy: <c>/pipelines/query</c>, <c>pipelines:read</c>.</summary>
    protected override string ResourceName => "pipelines";
}
