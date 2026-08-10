using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Web.RestEndpoints.Logging;

namespace Fdw.Schema.Endpoints.Discovery;

/// <summary>
/// Abstract base endpoint for retrieving schema graph data for ER diagram visualization.
/// Implementers must override <see cref="DiscoverContainers"/> to provide
/// connection-specific schema discovery logic.
/// </summary>
/// <remarks>
/// This base class handles graph conversion, auto-layout, error handling, and HTTP response
/// formatting. Subclasses implement connection resolution and schema discovery.
/// </remarks>
public abstract class GetSchemaGraphEndpointBase : Endpoint<GetSchemaGraphRequest, SchemaGraphResponse>
{
    private readonly ILogger<GetSchemaGraphEndpointBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSchemaGraphEndpointBase"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    protected GetSchemaGraphEndpointBase(ILogger<GetSchemaGraphEndpointBase>? logger = null)
    {
        _logger = logger ?? NullLogger<GetSchemaGraphEndpointBase>.Instance;
    }

    /// <summary>
    /// Gets the route for this endpoint. Default is "/connections/{ConnectionName}/schema-graph".
    /// </summary>
    protected virtual string Route => "/connections/{ConnectionName}/schema-graph";

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
            s.Summary = "Get schema graph for ER diagram";
            s.Description = "Returns schema information in a graph format suitable for ER diagram visualization.";
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
    /// Handles the request by discovering schema and converting to graph format.
    /// </summary>
    public override async Task HandleAsync(GetSchemaGraphRequest req, CancellationToken ct)
    {
        SchemaEndpointLog.DiscoveringSchema(_logger, req.ConnectionName);

        try
        {
            var containersResult = await DiscoverContainers(req.ConnectionName, ct).ConfigureAwait(false);

            if (!containersResult.IsSuccess || containersResult.Value == null)
            {
                SchemaEndpointLog.SchemaConnectionNotFound(_logger, req.ConnectionName, "schema-graph");
                AddError(containersResult.CurrentMessage ?? "Schema discovery failed");
                await Send.ErrorsAsync(containersResult.IsSuccess ? 404 : 400, ct).ConfigureAwait(false);
                return;
            }

            var containers = containersResult.Value;
            var graph = ConvertToSchemaGraph(req, containers);

            SchemaEndpointLog.SchemaDiscovered(_logger, req.ConnectionName, graph.Entities.Count);
            await Send.OkAsync(graph, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            SchemaEndpointLog.SchemaOperationFailed(_logger, ex, "schema-graph", req.ConnectionName);
            AddError("Schema graph retrieval failed");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Discovers storage containers for the specified connection.
    /// Implementers should resolve the connection, verify it supports discovery,
    /// and return the discovered containers.
    /// </summary>
    /// <param name="connectionName">The connection name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the discovered storage containers.</returns>
    protected abstract Task<IGenericResult<IReadOnlyList<IStorageContainer>>> DiscoverContainers(
        string connectionName,
        CancellationToken ct);

    /// <summary>
    /// Converts discovered storage containers into a schema graph response.
    /// Override to customize the graph conversion logic.
    /// </summary>
    /// <param name="req">The original request.</param>
    /// <param name="containers">The discovered storage containers.</param>
    /// <returns>The schema graph response.</returns>
    protected virtual SchemaGraphResponse ConvertToSchemaGraph(
        GetSchemaGraphRequest req,
        IReadOnlyList<IStorageContainer> containers)
    {
        var graph = new SchemaGraphResponse
        {
            ConnectionName = req.ConnectionName,
            SchemaName = req.SchemaFilter
        };

        var entityIndex = 0;

        foreach (var container in containers)
        {
            var schemaName = container.Path.PathValue;

            if (!string.IsNullOrEmpty(req.SchemaFilter) &&
                !string.Equals(schemaName, req.SchemaFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var entity = CreateEntity(schemaName, container, entityIndex++);
            graph.Entities.Add(entity);
        }

        AutoLayoutEntities(graph.Entities);

        return graph;
    }

    /// <summary>
    /// Creates a schema graph entity from a storage container.
    /// </summary>
    /// <param name="schemaName">The schema name.</param>
    /// <param name="container">The storage container.</param>
    /// <param name="index">The entity index for layout.</param>
    /// <returns>A schema graph entity DTO.</returns>
    protected virtual SchemaGraphEntityDto CreateEntity(string schemaName, IStorageContainer container, int index)
    {
        return new SchemaGraphEntityDto
        {
            FullName = $"{schemaName}.{container.Name}",
            Schema = schemaName,
            TableName = container.Name,
            EntityType = container.ContainerType.Name,
            Fields = container.Schema.Fields.Select((f, i) => new SchemaGraphFieldDto
            {
                Name = f.Name,
                DataType = f.FieldType.TypeName,
                IsNullable = f.IsNullable,
                // Why: IsPrimaryKey removed from IField — PK identity is now in DataContainerKeyField entries.
                // Schema graph uses the container's SurrogateKeyFieldNames for key identification.
                IsForeignKey = false,
                IsIdentity = f.IsIdentity,
                OrdinalPosition = i
            }).ToList(),
            Position = new SchemaGraphPositionDto { X = 0, Y = 0 }
        };
    }

    /// <summary>
    /// Automatically lays out entities in a grid pattern.
    /// </summary>
    /// <param name="entities">The entities to layout.</param>
    protected static void AutoLayoutEntities(IList<SchemaGraphEntityDto> entities)
    {
        if (entities.Count == 0) return;

        var cols = (int)Math.Ceiling(Math.Sqrt(entities.Count));
        var spacingX = 300;
        var spacingY = 350;

        for (var i = 0; i < entities.Count; i++)
        {
            var col = i % cols;
            var row = i / cols;
            entities[i].Position = new SchemaGraphPositionDto
            {
                X = col * spacingX + 50,
                Y = row * spacingY + 50
            };
        }
    }
}
