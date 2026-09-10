using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Pipelines;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Generic base endpoint for listing all configured pipelines.
/// Reads from <see cref="PipelineServiceConfigurationProvider"/> — the domain-owned gateway path
/// over pipe.Pipeline, with full [GenerateMapper] support for DataGateway queries.
/// </summary>
public abstract class ListPipelinesEndpointBase : CrudListEndpointBase<PipelineSummaryResponse>
{
    private readonly PipelineServiceConfigurationProvider _configProvider;

    /// <inheritdoc />
    protected ListPipelinesEndpointBase(ILogger<ListPipelinesEndpointBase> logger, PipelineServiceConfigurationProvider configProvider) : base(logger)
    {
        _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "pipelines";

    /// <summary>Loads all pipeline configurations and maps them to summary DTOs.</summary>
    protected override async Task<IGenericResult<List<PipelineSummaryResponse>>> LoadItems(CancellationToken ct)
    {
        var result = await _configProvider.Get(ct).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return result.ToNewResult<List<PipelineSummaryResponse>>();
        }

        var items = new List<PipelineSummaryResponse>();
        foreach (var config in (result.Value ?? []).Where(p => !string.IsNullOrWhiteSpace(p.Name)))
        {
            if (string.IsNullOrEmpty(config.Implementation))
                return GenericResult<List<PipelineSummaryResponse>>.Failure(
                    Logging.PipelineEndpointLog.PipelineMissingKind(Logger, config.Name));

            items.Add(MapToSummary(config));
        }

        return GenericResult<List<PipelineSummaryResponse>>.Success(items);
    }

    /// <summary>Maps a pipeline configuration to a summary DTO. Caller guarantees Implementation is set.</summary>
    protected virtual PipelineSummaryResponse MapToSummary(IPipelineImplementationConfiguration config)
    {
        return new PipelineSummaryResponse
        {
            Id = config.Id,
            Name = config.Name,
            PipelineType = config.Implementation!
        };
    }
}
