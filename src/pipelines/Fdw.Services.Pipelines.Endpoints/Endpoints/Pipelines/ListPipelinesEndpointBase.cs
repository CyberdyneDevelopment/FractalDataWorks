using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Web.RestEndpoints.Models;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Tags;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Base endpoint for listing configured pipelines, one page at a time (FDW-951).
/// Reads from <see cref="PipelineServiceConfigurationProvider"/> — the domain-owned gateway path
/// over pipe.Pipeline.
/// </summary>
/// <remarks>
/// A page is one read of the pipelines domain table: what a pipeline's summary shows -- its id, its name and
/// its kind, which is the domain row's implementation -- is all on the domain row, so no pipeline's
/// implementation or stages are read to list it (FDW-1003). The domain adds no field to its row.
/// </remarks>
public abstract class ListPipelinesEndpointBase : CrudPageEndpointBase<DomainConfiguration, PipelineSummaryResponse>
{
    private readonly PipelineServiceConfigurationProvider _configProvider;
    private readonly ITagService? _tags;

    /// <summary>Initializes a new instance of the <see cref="ListPipelinesEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configProvider">Reads the pipelines.</param>
    /// <param name="tags">The Tags service that answers <c>?tag=</c>; absent on a host that does not run it, which then refuses a request that sends <c>tag</c> (FDW-3816).</param>
    protected ListPipelinesEndpointBase(
        ILogger<ListPipelinesEndpointBase> logger,
        PipelineServiceConfigurationProvider configProvider,
        ITagService? tags = null) : base(logger)
    {
        _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        _tags = tags;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "pipelines";

    /// <inheritdoc />
    /// <remarks><c>?tag=</c> keeps the pipelines that carry every tag named (FDW-3816).</remarks>
    protected override async Task<IGenericResult<ConfigurationPage<DomainConfiguration>>> LoadPage(
        PaginatedListRequest paging, CancellationToken ct)
    {
        var selection = await TagSelection.Read(TaggedWith, _tags, "Pipeline", Logger, ct).ConfigureAwait(false);
        if (selection.IsFailure || selection.Value is null)
            return selection.ToNewResult<ConfigurationPage<DomainConfiguration>>();

        return await selection.Value.Read(
            (skip, take, token) => _configProvider.Get(new DomainQuery(null, null, skip, take, false), token),
            pipeline => pipeline.Id,
            paging.Skip,
            paging.Take,
            ct).ConfigureAwait(false);
    }

    /// <summary>Leaves out pipelines with no name.</summary>
    /// <param name="item">The pipeline.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>True to list the pipeline.</returns>
    protected override ValueTask<bool> Include(DomainConfiguration item, CancellationToken ct)
        => ValueTask.FromResult(!string.IsNullOrWhiteSpace(item.Name));

    /// <summary>Maps a pipeline to its summary; a pipeline with no kind fails the page.</summary>
    /// <param name="item">The pipeline.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The summary, or the failure that ends the page.</returns>
    protected override Task<IGenericResult<PipelineSummaryResponse>> ToSummary(DomainConfiguration item, CancellationToken ct)
        => Task.FromResult<IGenericResult<PipelineSummaryResponse>>(string.IsNullOrEmpty(item.Implementation)
            ? GenericResult<PipelineSummaryResponse>.Failure(Logging.PipelineEndpointLog.PipelineMissingKind(Logger, item.Name))
            : GenericResult<PipelineSummaryResponse>.Success(MapToSummary(item)));

    /// <summary>Maps a pipeline to its summary.</summary>
    /// <param name="config">The pipeline, as the pipelines domain table holds it.</param>
    /// <returns>The summary.</returns>
    protected virtual PipelineSummaryResponse MapToSummary(DomainConfiguration config)
    {
        return new PipelineSummaryResponse
        {
            Id = config.Id,
            Name = config.Name,
            PipelineType = config.Implementation!
        };
    }
}
