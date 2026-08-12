using Fdw.Services.Data.Clients.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Fdw.Web.RestEndpoints.Logging;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// Endpoint to get the full dataflow graph.
/// </summary>
public abstract class GetDataflowGraphEndpoint : EndpointWithoutRequest<DataflowGraphResponse>
{
    private readonly DataflowGraphConfigurationProvider _provider;
    private readonly ILogger<GetDataflowGraphEndpoint> _logger;

    /// <inheritdoc />
    protected GetDataflowGraphEndpoint(DataflowGraphConfigurationProvider provider, ILogger<GetDataflowGraphEndpoint> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/dataflow/graph");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:read");
#endif
        Summary(s =>
        {
            s.Summary = "Get dataflow graph";
            s.Description = "Returns the complete dataflow graph showing relationships between DataSets, DataStores, Connections, and Sources.";
        });
    }

    /// <summary>Builds and returns the complete dataflow graph with nodes, edges, and statistics.</summary>
    public override async Task HandleAsync(CancellationToken ct)
    {
        EndpointLog.ListingResources(_logger, "dataflow graph");

        // Why: callers can scope the graph with ?pipelineName=. When supplied, validate the
        // pipeline exists so a typo gets a 404 instead of a misleading full-graph 200.
        var pipelineFilter = Query<string>("pipelineName", isRequired: false);
        if (!string.IsNullOrWhiteSpace(pipelineFilter))
        {
            var existsResult = await _provider.PipelineExists(pipelineFilter, ct).ConfigureAwait(false);
            if (!existsResult.IsSuccess || !existsResult.Value)
            {
                DataflowGraphConfigurationProviderLog.PipelineNotFound(_logger, pipelineFilter);
                HttpContext.Response.StatusCode = 404;
                HttpContext.Response.ContentType = "application/json";
                await HttpContext.Response.WriteAsJsonAsync(new
                {
                    errorCode = "NotFound",
                    messages = new[] { $"Pipeline '{pipelineFilter}' was not found." }
                }, ct).ConfigureAwait(false);
                return;
            }
        }

        var nodes = new List<DataflowNodeDto>();
        var edges = new List<DataflowEdgeDto>();
        var stats = new DataflowStatsDto();

        var dataSetsResult = await _provider.LoadDataSets(ct).ConfigureAwait(false);
        IReadOnlyList<DataSetRecord> dataSets = dataSetsResult.IsSuccess ? dataSetsResult.Value ?? [] : [];
        stats.DataSetCount = dataSets.Count;
        AddDataSetNodes(dataSets, nodes);

        var dataStoresResult = await _provider.LoadDataStores(ct).ConfigureAwait(false);
        IReadOnlyList<DataStoreRecord> dataStores = dataStoresResult.IsSuccess ? dataStoresResult.Value ?? [] : [];
        stats.DataStoreCount = dataStores.Count;
        AddDataStoreNodes(dataStores, nodes);

        var sourcesResult = await _provider.LoadSources(ct).ConfigureAwait(false);
        IReadOnlyList<DataSetSourcePayload> sources = sourcesResult.IsSuccess ? sourcesResult.Value ?? [] : [];
        stats.SourceCount = sources.Count;

        var connectionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddSourceNodesAndEdges(sources, dataSets, dataStores, nodes, edges, connectionNames);

        AddConnectionNodes(connectionNames, nodes);
        stats.ConnectionCount = connectionNames.Count;

        AddSourceToConnectionEdges(sources, edges);

        DataflowGraphConfigurationProviderLog.Loaded(_logger, dataSets.Count, dataStores.Count, sources.Count);
        stats.EdgeCount = edges.Count;

        await Send.OkAsync(new DataflowGraphResponse
        {
            Nodes = nodes,
            Edges = edges,
            Stats = stats
        }, ct).ConfigureAwait(false);
    }

    /// <summary>Adds DataSet nodes to the graph.</summary>
    protected virtual void AddDataSetNodes(IReadOnlyList<DataSetRecord> dataSets, IList<DataflowNodeDto> nodes)
    {
        foreach (var ds in dataSets)
        {
            nodes.Add(new DataflowNodeDto
            {
                Id = $"dataset_{ds.Id}",
                Label = ds.Name,
                NodeType = "dataset",
                Category = ds.Category,
                Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["version"] = ds.Version,
                    ["description"] = ds.Description ?? "",
                    ["recordType"] = ds.RecordTypeName ?? ""
                }
            });
        }
    }

    /// <summary>Adds DataStore nodes to the graph.</summary>
    protected virtual void AddDataStoreNodes(IReadOnlyList<DataStoreRecord> dataStores, IList<DataflowNodeDto> nodes)
    {
        foreach (var store in dataStores)
        {
            nodes.Add(new DataflowNodeDto
            {
                Id = $"datastore_{store.ConfigurationId}",
                Label = store.DataStoreTypeName ?? store.StoreType,
                NodeType = "datastore",
                Category = store.StoreType,
                Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["storeType"] = store.StoreType,
                    ["location"] = store.Location,
                    ["translatorType"] = store.TranslatorType
                }
            });
        }
    }

    /// <summary>Adds source nodes and edges linking sources to DataSets, DataStores, and connections.</summary>
    protected virtual void AddSourceNodesAndEdges(
        IReadOnlyList<DataSetSourcePayload> sources,
        IReadOnlyList<DataSetRecord> dataSets,
        IReadOnlyList<DataStoreRecord> dataStores,
        IList<DataflowNodeDto> nodes,
        IList<DataflowEdgeDto> edges,
        ISet<string> connectionNames)
    {
        foreach (var source in sources)
        {
            var sourceNodeId = $"source_{source.Id}";
            nodes.Add(new DataflowNodeDto
            {
                Id = sourceNodeId,
                Label = source.SourceName,
                NodeType = "source",
                Category = source.ConnectionType,
                Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["connectionName"] = source.ConnectionName ?? "",
                    ["dataStoreName"] = source.DataStoreName ?? "",
                    ["priority"] = source.Priority,
                    ["path"] = source.PathName ?? "",
                    ["containerName"] = source.ContainerName ?? "",
                    ["httpEndpoint"] = source.HttpEndpoint ?? "",
                    ["filePath"] = source.FilePath ?? ""
                }
            });

            var dataSetNode = dataSets.FirstOrDefault(d => d.Id == source.DataSetId);
            if (dataSetNode != null)
            {
                edges.Add(new DataflowEdgeDto
                {
                    Id = $"edge_ds_{source.DataSetId}_src_{source.Id}",
                    Source = $"dataset_{source.DataSetId}",
                    Target = sourceNodeId,
                    RelationType = "uses_source",
                    Label = $"Priority: {source.Priority}",
                    Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["priority"] = source.Priority
                    }
                });
            }

            if (!string.IsNullOrEmpty(source.ConnectionName))
                connectionNames.Add(source.ConnectionName);

            if (!string.IsNullOrEmpty(source.DataStoreName))
            {
                var dataStore = dataStores.FirstOrDefault(ds =>
                    string.Equals(ds.DataStoreTypeName, source.DataStoreName, StringComparison.OrdinalIgnoreCase));
                if (dataStore != null)
                {
                    edges.Add(new DataflowEdgeDto
                    {
                        Id = $"edge_src_{source.Id}_store_{dataStore.ConfigurationId}",
                        Source = sourceNodeId,
                        Target = $"datastore_{dataStore.ConfigurationId}",
                        RelationType = "stored_in",
                        Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
                    });
                }
            }
        }
    }

    /// <summary>Adds connection nodes to the graph for each unique connection name.</summary>
    protected virtual void AddConnectionNodes(IReadOnlySet<string> connectionNames, IList<DataflowNodeDto> nodes)
    {
        foreach (var connName in connectionNames)
        {
            nodes.Add(new DataflowNodeDto
            {
                Id = $"connection_{connName}",
                Label = connName,
                NodeType = "connection",
                Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
            });
        }
    }

    /// <summary>Adds edges linking sources to their connection nodes.</summary>
    protected virtual void AddSourceToConnectionEdges(IReadOnlyList<DataSetSourcePayload> sources, IList<DataflowEdgeDto> edges)
    {
        foreach (var source in sources.Where(s => !string.IsNullOrEmpty(s.ConnectionName)))
        {
            edges.Add(new DataflowEdgeDto
            {
                Id = $"edge_src_{source.Id}_conn_{source.ConnectionName}",
                Source = $"source_{source.Id}",
                Target = $"connection_{source.ConnectionName}",
                RelationType = "uses_connection",
                Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
            });
        }
    }
}
