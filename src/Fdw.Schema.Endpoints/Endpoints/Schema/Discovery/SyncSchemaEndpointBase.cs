using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Schema.Clients.Models;
using Fdw.Web.RestEndpoints.Logging;

namespace Fdw.Schema.Endpoints.Discovery;

/// <summary>
/// Abstract base endpoint for synchronizing database schema with stored DataStore configuration.
/// Implementers must override <see cref="SyncSchema"/> to provide the actual
/// comparison and synchronization logic.
/// </summary>
/// <remarks>
/// This base class handles request validation, logging, error handling, and HTTP response
/// formatting. Subclasses implement connection resolution, live schema discovery,
/// stored schema comparison, and optional change application.
/// </remarks>
public abstract class SyncSchemaEndpointBase : Endpoint<SyncSchemaRequest, SyncSchemaResponse>
{
    private readonly ILogger<SyncSchemaEndpointBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncSchemaEndpointBase"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    protected SyncSchemaEndpointBase(ILogger<SyncSchemaEndpointBase>? logger = null)
    {
        _logger = logger ?? NullLogger<SyncSchemaEndpointBase>.Instance;
    }

    /// <summary>
    /// Gets the route for this endpoint. Default is "/connections/{ConnectionName}/sync-schema".
    /// </summary>
    protected virtual string Route => "/connections/{ConnectionName}/sync-schema";

    /// <summary>
    /// Gets the policy name for authorization. Default is "schema:write".
    /// </summary>
    protected virtual string PolicyName => "schema:write";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Post(Route);
#if DEVELOP
        AllowAnonymous();
#else
        Policies(PolicyName);
#endif
        Summary(s =>
        {
            s.Summary = "Sync schema changes";
            s.Description = "Compares current database schema with stored configuration and reports drift. Optionally applies changes.";
        });

        OnBeforeConfiguring();
    }

    /// <summary>
    /// Virtual hook called at the end of <see cref="Configure"/>.
    /// Override to add additional endpoint configuration.
    /// </summary>
    protected virtual void OnBeforeConfiguring()
    {
    }

    /// <summary>
    /// Handles the request by performing schema synchronization.
    /// </summary>
    public override async Task HandleAsync(SyncSchemaRequest req, CancellationToken ct)
    {
        SchemaEndpointLog.SyncingSchema(_logger, req.ConnectionName);

        try
        {
            await OnSyncing(req, ct).ConfigureAwait(false);

            var result = await SyncSchema(req, ct).ConfigureAwait(false);

            if (!result.IsSuccess || result.Value == null)
            {
                var message = result.CurrentMessage ?? "Schema sync failed";
                SchemaEndpointLog.SchemaConnectionNotFound(_logger, req.ConnectionName, "sync");
                AddError(message);
                await Send.ErrorsAsync(result.IsSuccess ? 404 : 400, ct).ConfigureAwait(false);
                return;
            }

            var response = result.Value;
            SchemaEndpointLog.SchemaSynced(
                _logger,
                req.ConnectionName,
                response.AddedTables.Count,
                response.ModifiedTables.Count,
                response.RemovedTables.Count);

            await OnSynced(response, ct).ConfigureAwait(false);
            await Send.OkAsync(response, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            SchemaEndpointLog.SchemaOperationFailed(_logger, ex, "sync", req.ConnectionName);
            AddError("Schema sync failed");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Performs schema synchronization for the specified connection.
    /// Implementers should resolve the connection, discover the live schema,
    /// compare with stored configuration, and optionally apply changes.
    /// </summary>
    /// <param name="request">The sync request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the sync response, or failure information.</returns>
    protected abstract Task<IGenericResult<SyncSchemaResponse>> SyncSchema(
        SyncSchemaRequest request,
        CancellationToken ct);

    /// <summary>
    /// Virtual hook called before sync starts.
    /// Override to add custom validation or pre-processing logic.
    /// </summary>
    /// <param name="request">The sync request.</param>
    /// <param name="ct">Cancellation token.</param>
    protected virtual Task OnSyncing(SyncSchemaRequest request, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Virtual hook called after sync succeeds, before sending the response.
    /// Override to add post-processing such as cache invalidation or notifications.
    /// </summary>
    /// <param name="response">The sync response.</param>
    /// <param name="ct">Cancellation token.</param>
    protected virtual Task OnSynced(SyncSchemaResponse response, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
