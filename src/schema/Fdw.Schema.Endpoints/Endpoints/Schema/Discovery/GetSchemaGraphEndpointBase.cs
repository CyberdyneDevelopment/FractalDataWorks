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
using Fdw.Web.RestEndpoints.ErrorMapping;

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

            if (containersResult.IsFailure)
            {
                await Send.ResultAsync(ResultHttpStatusMapper.Answer(containersResult, HttpContext)).ConfigureAwait(false);
                return;
            }

            if (containersResult.Value == null)
            {
                SchemaEndpointLog.SchemaConnectionNotFound(_logger, req.ConnectionName, "schema-graph");
                await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(SchemaEndpointLog.GetSchemaGraphConnectionNotFound(_logger, req.ConnectionName)), HttpContext)).ConfigureAwait(false);
                return;
            }

            var containers = containersResult.Value;
            var graph = ConvertToSchemaGraph(req, containers);
            AddWarnings(graph, containersResult);

            SchemaEndpointLog.SchemaDiscovered(_logger, req.ConnectionName, graph.Entities.Count);
            await Send.OkAsync(graph, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            SchemaEndpointLog.SchemaOperationFailed(_logger, ex, "schema-graph", req.ConnectionName);
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(SchemaEndpointLog.GetSchemaGraphFaulted(_logger)), HttpContext)).ConfigureAwait(false);
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
        var included = new List<IStorageContainer>();

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
            included.Add(container);
        }

        AddRelationships(graph, included);
        AutoLayoutEntities(graph.Entities);

        return graph;
    }

    /// <summary>
    /// Adds one relationship for each column pair of each foreign key whose container and target are both entities of the graph.
    /// A foreign key whose target the discovery did not read, or that the schema filter left out, draws none: its columns are still
    /// marked as foreign key columns on their entity, and the discovery's warning names the key.
    /// </summary>
    /// <param name="graph">The graph, whose entities are the containers shown.</param>
    /// <param name="containers">The containers shown, in the order of the entities.</param>
    protected virtual void AddRelationships(SchemaGraphResponse graph, IReadOnlyList<IStorageContainer> containers)
    {
        var shown = new HashSet<string>(graph.Entities.Select(entity => entity.FullName), StringComparer.OrdinalIgnoreCase);
        foreach (var container in containers)
        {
            var source = $"{container.Path.PathValue}.{container.Name}";
            foreach (var pair in container.ForeignKeyColumnPairs())
            {
                var target = $"{pair.ReferencedContainer.Path.PathValue}.{pair.ReferencedContainer.Name}";
                if (!shown.Contains(target))
                    continue;

                graph.Relationships.Add(new SchemaGraphRelationshipDto
                {
                    SourceEntity = source,
                    TargetEntity = target,
                    SourceColumn = pair.Column,
                    TargetColumn = pair.ReferencedColumn,
                });
            }
        }
    }

    /// <summary>
    /// Copies the warnings the discovery returned with its success onto the graph, so a graph drawn with less than the catalog holds
    /// says so.
    /// </summary>
    /// <param name="graph">The graph.</param>
    /// <param name="discovery">The discovery's result.</param>
    protected static void AddWarnings(SchemaGraphResponse graph, IGenericResult discovery)
    {
        foreach (var message in discovery.Messages)
        {
            graph.Warnings.Add(new SchemaGraphWarningDto { Code = message.Code ?? string.Empty, Message = message.Message });
        }
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
        var primaryKey = container.PrimaryKeyFieldNames();
        var foreignKey = container.ForeignKeyFieldNames();

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
                IsPrimaryKey = primaryKey.Contains(f.Name, StringComparer.OrdinalIgnoreCase),
                IsForeignKey = foreignKey.Contains(f.Name, StringComparer.OrdinalIgnoreCase),
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
