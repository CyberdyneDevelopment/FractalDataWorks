using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Web.RestEndpoints.Logging;

namespace Fdw.Schema.Endpoints.Discovery;

/// <summary>
/// Abstract base endpoint for listing connections that support schema discovery.
/// Implementers must override <see cref="GetSchemaCapableConnections"/> to provide
/// connection discovery logic.
/// </summary>
public abstract class ListSchemaCapableConnectionsEndpointBase : EndpointWithoutRequest<List<ConnectionInfoDto>>
{
    private readonly ILogger<ListSchemaCapableConnectionsEndpointBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListSchemaCapableConnectionsEndpointBase"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    protected ListSchemaCapableConnectionsEndpointBase(ILogger<ListSchemaCapableConnectionsEndpointBase>? logger = null)
    {
        _logger = logger ?? NullLogger<ListSchemaCapableConnectionsEndpointBase>.Instance;
    }

    /// <summary>
    /// Gets the route for this endpoint. Default is "/connections/schema-capable".
    /// </summary>
    protected virtual string Route => "/connections/schema-capable";

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
            s.Summary = "List schema-capable connections";
            s.Description = "Returns connections that support schema discovery (database connections).";
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
    /// Handles the request by retrieving and returning schema-capable connections.
    /// </summary>
    public override async Task HandleAsync(CancellationToken ct)
    {
        SchemaEndpointLog.ListingSchemaConnections(_logger);

        try
        {
            var connections = await GetSchemaCapableConnections(ct).ConfigureAwait(false);
            SchemaEndpointLog.SchemaConnectionsListed(_logger, connections.Count);
            await Send.OkAsync(connections, ct).ConfigureAwait(false);
        }
        catch (System.Exception ex)
        {
            SchemaEndpointLog.SchemaOperationFailed(_logger, ex, "list connections", "all");
            AddError("Failed to list schema-capable connections");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Gets the list of connections that support schema discovery.
    /// Implementers should query connection configurations and filter for schema-capable types.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of schema-capable connection info DTOs.</returns>
#pragma warning disable MA0016 // Return type constrained by endpoint generic parameter
    protected abstract Task<List<ConnectionInfoDto>> GetSchemaCapableConnections(CancellationToken ct);
#pragma warning restore MA0016
}
