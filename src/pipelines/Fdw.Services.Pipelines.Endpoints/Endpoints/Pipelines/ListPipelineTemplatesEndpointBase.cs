using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Services.Etl;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Lists the pipelines that are templates (FDW-3379), for the picker that makes a pipeline from one.
/// </summary>
/// <remarks>
/// It is the pipeline list with one more question asked of each pipeline: whether it is a template. A page holds the pipelines that are, so a page can be shorter than
/// the page size; the response says whether there are more.
/// </remarks>
public abstract class ListPipelineTemplatesEndpointBase : ListPipelinesEndpointBase
{
    private readonly PipelineServiceConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="ListPipelineTemplatesEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The pipeline domain's configuration provider.</param>
    protected ListPipelineTemplatesEndpointBase(ILogger<ListPipelineTemplatesEndpointBase> logger, PipelineServiceConfigurationProvider provider) : base(logger, provider)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    protected override string Route => "/pipelines/templates";

    /// <inheritdoc />
    protected override string EndpointSummary => "List pipeline templates";

    /// <inheritdoc />
    protected override string EndpointDescription => "Lists the pipelines that are templates: a new pipeline is made from one with From template.";

    /// <inheritdoc />
    protected override async ValueTask<bool> Include(DomainConfiguration item, CancellationToken ct)
    {
        if (!await base.Include(item, ct).ConfigureAwait(false))
        {
            return false;
        }

        var read = await _provider.Get(item.Name, ct).ConfigureAwait(false);
        return read.IsSuccess && read.Value is ITemplatePipelineConfiguration { IsTemplate: true };
    }
}
