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
    /// <summary>Initializes a new instance of the <see cref="BulkPipelineStatusEndpointBase"/> class.</summary>
        private readonly IConfigurationGatewayProvider _configurationGateways;

    /// <summary>Initializes a new instance of the <see cref="BulkPipelineStatusEndpointBase"/> class.</summary>
    protected BulkPipelineStatusEndpointBase(ILogger logger, IConfigurationGatewayProvider configurationGateways)
    {
        EndpointLogger = logger;
        _configurationGateways = configurationGateways ?? throw new ArgumentNullException(nameof(configurationGateways));
    }


    private static string PlatformConfigurationConnection => PlatformConnection.Name;

    /// <summary>
    /// Gets the gateway onto the platform's own configuration store, where pipeline status rows live.
    /// </summary>
    // The platform's own rows are read through the configuration gateway onto that store, never the request-facing
    // Main gateway (FDW-1068): Main runs whatever data store a request names, and these rows are not a request's to name.
    protected IDataGateway DataGateway => _configurationGateways.Get(PlatformConfigurationConnection) is { IsSuccess: true, Value: { } gateway }
        ? gateway
        : throw new InvalidOperationException(
            $"No configuration gateway is available for connection '{PlatformConfigurationConnection}'. It must be declared in the host's configurationSchema.json.");

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger EndpointLogger { get; }

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
        
        OnFetchingBulkStatus();

        var pipelines = new List<PipelineStatusInfo>();

        try
        {
            var command = new QueryCommand<PipelineStatusRecord>();

            var result = await DataGateway.Execute<IEnumerable<PipelineStatusRecord>>(
                command, new DataStoreTarget(PlatformConfigurationConnection, "etl", "Pipeline"), ct).ConfigureAwait(false);
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
