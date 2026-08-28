using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Commands.Data;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Endpoint to get status of all pipelines in a single call.
/// Solves N+1 problem for dashboard and nav menu.
/// </summary>
public abstract class BulkPipelineStatusEndpointBase : EndpointWithoutRequest<BulkPipelineStatusResponse>
{
    private readonly IDataGateway _dataGateway;

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkPipelineStatusEndpointBase"/> class.
    /// </summary>
    protected BulkPipelineStatusEndpointBase(IDataGateway dataGateway)
    {
        _dataGateway = dataGateway;
    }

    /// <summary>
    /// Gets the data gateway.
    /// </summary>
    protected IDataGateway DataGateway => _dataGateway;

    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/etl/pipelines/status");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:read");
#endif
        Summary(s =>
        {
            s.Summary = "Get status of all pipelines";
            s.Description = "Returns the current status for all configured pipelines in a single call.";
        });
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(CancellationToken ct)
    {
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        OnFetchingBulkStatus();

        var pipelines = new List<PipelineStatusInfo>();

        try
        {
            var command = new QueryCommand<PipelineStatusRecord>();

            var result = await _dataGateway.Execute<IEnumerable<PipelineStatusRecord>>(
                command, new DataStoreTarget("PlatformConfiguration", "etl", "Pipeline"), ct).ConfigureAwait(false);
            if (result.IsSuccess && result.Value != null)
            {
                pipelines = result.Value
                    .Select(MapToStatusInfo)
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            OnPipelineFetchFailed("database query", ex.Message);
        }

        OnBulkStatusRetrieved(pipelines.Count);

        await Send.OkAsync(new BulkPipelineStatusResponse { Pipelines = pipelines }, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Maps a database record to a pipeline status info DTO.
    /// </summary>
    protected virtual PipelineStatusInfo MapToStatusInfo(PipelineStatusRecord record)
    {
        return new PipelineStatusInfo
        {
            Id = record.Id,
            Name = record.Name,
            PipelineType = record.PipelineType,
            IsExecuting = false // Status not tracked in DB currently
        };
    }

    /// <summary>Called when fetching bulk status. Override for custom logging.</summary>
    protected virtual void OnFetchingBulkStatus() { }

    /// <summary>Called when pipeline fetch fails. Override for custom logging.</summary>
    protected virtual void OnPipelineFetchFailed(string context, string error) { }

    /// <summary>Called when bulk status is retrieved. Override for custom logging.</summary>
    protected virtual void OnBulkStatusRetrieved(int count) { }

    /// <summary>
    /// Database record for pipeline status query.
    /// </summary>
    protected sealed record PipelineStatusRecord(Guid Id, string Name, string PipelineType);
}
