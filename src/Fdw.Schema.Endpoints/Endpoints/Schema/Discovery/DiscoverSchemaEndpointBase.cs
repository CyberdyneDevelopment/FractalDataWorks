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
/// Abstract base endpoint for discovering database schema from a connection.
/// Implementers must override <see cref="DiscoverSchema"/> to provide
/// connection-specific schema discovery logic.
/// </summary>
/// <remarks>
/// This base class handles request validation, logging, error handling, and HTTP response
/// formatting. Subclasses only need to implement the actual schema discovery logic.
/// </remarks>
public abstract class DiscoverSchemaEndpointBase : Endpoint<DiscoverSchemaRequest, SchemaDiscoveryResponse>
{
    private readonly ILogger<DiscoverSchemaEndpointBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoverSchemaEndpointBase"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    protected DiscoverSchemaEndpointBase(ILogger<DiscoverSchemaEndpointBase>? logger = null)
    {
        _logger = logger ?? NullLogger<DiscoverSchemaEndpointBase>.Instance;
    }

    /// <summary>
    /// Gets the route for this endpoint. Default is "/connections/{ConnectionName}/schema".
    /// </summary>
    protected virtual string Route => "/connections/{ConnectionName}/schema";

    /// <summary>
    /// Gets the policy name for authorization. Default is "schema:read".
    /// </summary>
    protected virtual string PolicyName => "schema:read";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get(Route);
#if DEVELOP
        AllowAnonymous();
#else
        Policies(PolicyName);
#endif
        Summary(s =>
        {
            s.Summary = "Discover connection schema";
            s.Description = "Retrieves schema information (tables, views, columns) from a database connection.";
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
    /// Handles the request by validating the connection and performing schema discovery.
    /// </summary>
    public override async Task HandleAsync(DiscoverSchemaRequest req, CancellationToken ct)
    {
        SchemaEndpointLog.DiscoveringSchema(_logger, req.ConnectionName);

        try
        {
            await OnDiscovering(req, ct).ConfigureAwait(false);

            var result = await DiscoverSchema(req.ConnectionName, ct).ConfigureAwait(false);

            if (!result.IsSuccess || result.Value == null)
            {
                var message = result.CurrentMessage ?? "Schema discovery failed";
                SchemaEndpointLog.SchemaConnectionNotFound(_logger, req.ConnectionName, "discover");
                AddError(message);
                await Send.ErrorsAsync(result.IsSuccess ? 404 : 400, ct).ConfigureAwait(false);
                return;
            }

            var response = result.Value;
            SchemaEndpointLog.SchemaDiscovered(_logger, req.ConnectionName, response.Schemas.Count);

            await OnDiscovered(response, ct).ConfigureAwait(false);
            await Send.OkAsync(response, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            SchemaEndpointLog.SchemaOperationFailed(_logger, ex, "discover", req.ConnectionName);
            AddError("Schema discovery failed");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Performs schema discovery for the specified connection.
    /// Implementers should resolve the connection, verify it supports discovery,
    /// and return the discovered schema information.
    /// </summary>
    /// <param name="connectionName">The name of the connection to discover.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the schema discovery response, or failure information.</returns>
    protected abstract Task<IGenericResult<SchemaDiscoveryResponse>> DiscoverSchema(
        string connectionName,
        CancellationToken ct);

    /// <summary>
    /// Virtual hook called before discovery starts.
    /// Override to add custom validation or pre-processing logic.
    /// </summary>
    /// <param name="request">The discovery request.</param>
    /// <param name="ct">Cancellation token.</param>
    protected virtual Task OnDiscovering(DiscoverSchemaRequest request, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Virtual hook called after discovery succeeds, before sending the response.
    /// Override to enrich or transform the response.
    /// </summary>
    /// <param name="response">The discovery response.</param>
    /// <param name="ct">Cancellation token.</param>
    protected virtual Task OnDiscovered(SchemaDiscoveryResponse response, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
