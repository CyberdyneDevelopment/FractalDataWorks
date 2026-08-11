using Fdw.Services.Data.Clients.Models;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Services.Pipelines;
// DataSetRecord and DataSetSourcePayload now in this namespace
// ApiEndpointLog now in this namespace
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Web.RestEndpoints.Logging;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// Endpoint to get lineage for a specific DataSet.
/// </summary>
public abstract class GetDataSetLineageEndpoint : Endpoint<DataSetLineageRequest, DataSetLineageResponse>
{
    // Why: IConfigurationGateway routes directly to ConfigurationDb via configurationSchema.json.
    // Using plain IDataGateway would look for "ConfigurationDb" in the runtime DataStore table
    // (data.DataStore), where it does not exist — it is only a bootstrap connection in the JSON.
    private readonly IConfigurationGateway _configurationGateway;
    private readonly ILogger<GetDataSetLineageEndpoint> _logger;
    // Why: same composing-provider mechanism as GetLineageGraphEndpointBase. Also fixes the
    // "downstream consumers always empty" bug — the OLD code filtered pipe.Pipeline on
    // SourceDataSet/DestinationDataSet columns that do not exist on that table (SQL "Invalid column
    // name", swallowed into an empty list by the existing best-effort QueryAll pattern).
    private readonly PipelineServiceConfigurationProvider _pipelineProvider;

    /// <inheritdoc />
    protected GetDataSetLineageEndpoint(
        IConfigurationGateway configurationGateway,
        PipelineServiceConfigurationProvider pipelineProvider,
        ILogger<GetDataSetLineageEndpoint> logger)
    {
        _configurationGateway = configurationGateway;
        _pipelineProvider = pipelineProvider;
        _logger = logger ?? NullLogger<GetDataSetLineageEndpoint>.Instance;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/dataflow/lineage/{Name}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:read");
#endif
        Summary(s =>
        {
            s.Summary = "Get DataSet lineage";
            s.Description = "Returns upstream sources and downstream consumers for a specific DataSet.";
        });
    }

    /// <summary>Handles the lineage request by building upstream sources, downstream consumers, and field-level lineage.</summary>
    public override async Task HandleAsync(DataSetLineageRequest req, CancellationToken ct)
    {
        EndpointLog.GettingResource(_logger, "DataSet lineage", req.Name);

        var dataSet = await FindDataSet(req.Name, ct).ConfigureAwait(false);
        if (dataSet == null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var sources = await GetSources(dataSet.Id, ct).ConfigureAwait(false);
        var upstreamSources = BuildUpstreamSources(sources);
        var fieldLineage = await BuildFieldLineage(sources, ct).ConfigureAwait(false);
        var downstreamConsumers = await BuildDownstreamConsumers(req.Name, ct).ConfigureAwait(false);

        await Send.OkAsync(new DataSetLineageResponse
        {
            DataSetName = req.Name,
            UpstreamSources = upstreamSources.ToList(),
            DownstreamConsumers = downstreamConsumers.ToList(),
            FieldLineage = fieldLineage.ToList()
        }, ct).ConfigureAwait(false);
    }

    /// <summary>Finds a DataSet by name from the configuration database.</summary>
    protected virtual async Task<DataSetRecord?> FindDataSet(string name, CancellationToken ct)
    {
        // Why: Addressing moved off IDataCommand onto DataStoreTarget.
        var command = new QueryCommand<DataSetRecord>
        {
            Filter = new FilterExpression
            {
                Root = new FilterCondition
                {
                    PropertyName = "Name",
                    Operator = FilterOperators.ByName("Equal"),
                    Value = name
                }
            }
        };

        var result = await _configurationGateway.Execute<IEnumerable<DataSetRecord>>(
            command, new DataStoreTarget("ConfigurationDb", "data", "DataSet"), ct).ConfigureAwait(false);
        return result.IsSuccess ? result.Value?.FirstOrDefault() : null;
    }

    /// <summary>Retrieves all sources for a DataSet, ordered by priority.</summary>
    protected virtual async Task<IReadOnlyList<DataSetSourcePayload>> GetSources(Guid dataSetId, CancellationToken ct)
    {
        // Why: Addressing moved off IDataCommand onto DataStoreTarget.
        var command = new QueryCommand<DataSetSourcePayload>
        {
            Filter = new FilterExpression
            {
                Root = new FilterCondition
                {
                    PropertyName = "DataSetId",
                    Operator = FilterOperators.ByName("Equal"),
                    Value = dataSetId
                }
            },
            Ordering = new OrderingExpression
            {
                OrderedFields = [new OrderedField { PropertyName = "Priority", Direction = SortDirections.ByName("Ascending") }]
            }
        };

        var result = await _configurationGateway.Execute<IEnumerable<DataSetSourcePayload>>(
            command, new DataStoreTarget("ConfigurationDb", "data", "DataSetSource"), ct).ConfigureAwait(false);
        return result.IsSuccess ? result.Value?.ToList() ?? [] : [];
    }

    /// <summary>Builds upstream source DTOs from source records, classifying source types by their physical location.</summary>
    protected virtual IReadOnlyList<LineageSourceResponse> BuildUpstreamSources(IReadOnlyList<DataSetSourcePayload> sources)
    {
        return sources.Select(s =>
        {
            var sourceType = "Unknown";
            var physicalLocation = "";

            if (!string.IsNullOrEmpty(s.ContainerName))
            {
                sourceType = "Database";
                physicalLocation = !string.IsNullOrEmpty(s.Path)
                    ? $"{s.Path}.{s.ContainerName}"
                    : s.ContainerName;
            }
            else if (!string.IsNullOrEmpty(s.HttpEndpoint))
            {
                sourceType = "REST API";
                physicalLocation = s.HttpEndpoint;
            }
            else if (!string.IsNullOrEmpty(s.FilePath))
            {
                sourceType = "File";
                physicalLocation = s.FilePath;
            }

            return new LineageSourceResponse
            {
                Name = s.SourceName,
                SourceType = sourceType,
                ConnectionName = s.ConnectionName,
                DataStoreName = s.DataStoreName,
                PhysicalLocation = physicalLocation,
                Priority = s.Priority
            };
        }).ToList();
    }

    /// <summary>Builds field-level lineage by loading field mappings for each source.</summary>
    protected virtual async Task<IReadOnlyList<FieldLineageResponse>> BuildFieldLineage(IReadOnlyList<DataSetSourcePayload> sources, CancellationToken ct)
    {
        var fieldLineage = new List<FieldLineageResponse>();

        foreach (var source in sources)
        {
            // Why: Addressing moved off IDataCommand onto DataStoreTarget.
            var mappingsCommand = new QueryCommand<DataSetFieldMappingRecord>
            {
                Filter = new FilterExpression
                {
                    Root = new FilterCondition
                    {
                        PropertyName = "DataSetSourceId",
                        Operator = FilterOperators.ByName("Equal"),
                        Value = source.Id
                    }
                }
            };

            var mappingsResult = await _configurationGateway.Execute<IEnumerable<DataSetFieldMappingRecord>>(
                mappingsCommand, new DataStoreTarget("ConfigurationDb", "data", "DataSetFieldMapping"), ct).ConfigureAwait(false);
            var mappings = mappingsResult.IsSuccess ? mappingsResult.Value?.ToList() ?? [] : [];

            foreach (var mapping in mappings)
            {
                var existingField = fieldLineage.FirstOrDefault(f =>
                    f.LogicalField.Equals(mapping.LogicalFieldName, StringComparison.OrdinalIgnoreCase));

                if (existingField == null)
                {
                    existingField = new FieldLineageResponse
                    {
                        LogicalField = mapping.LogicalFieldName,
                        Sources = []
                    };
                    fieldLineage.Add(existingField);
                }

                existingField.Sources.Add(new FieldSourceMappingResponse
                {
                    SourceName = source.SourceName,
                    PhysicalField = mapping.PhysicalFieldName
                });
            }
        }

        return fieldLineage;
    }

    /// <summary>Identifies downstream consumers by finding pipelines that reference this DataSet.</summary>
    // Why: the OLD code filtered pipe.Pipeline directly on SourceDataSet/DestinationDataSet — columns
    // that live only on the engine typed body two levels down, not on the flat header row. Loading
    // through the composing provider and filtering in-memory is the fix (mirrors the graph-builder path).
    protected virtual async Task<IReadOnlyList<LineageConsumerResponse>> BuildDownstreamConsumers(string dataSetName, CancellationToken ct)
    {
        var downstreamConsumers = new List<LineageConsumerResponse>();

        var pipelines = await PipelineLineageLoader.Load(_pipelineProvider, _logger, ct).ConfigureAwait(false);

        var pipelinesAsSource = pipelines
            .Where(p => string.Equals(p.SourceDataSet, dataSetName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var pipelinesAsDest = pipelines
            .Where(p => string.Equals(p.DestinationDataSet, dataSetName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var pipeline in pipelinesAsSource)
        {
            downstreamConsumers.Add(new LineageConsumerResponse
            {
                Name = pipeline.Name,
                ConsumerType = $"Pipeline ({pipeline.ServiceOptionType})"
            });
        }

        foreach (var pipeline in pipelinesAsDest.Where(p => !pipelinesAsSource.Any(ps =>
            ps.Id == p.Id)))
        {
            downstreamConsumers.Add(new LineageConsumerResponse
            {
                Name = pipeline.Name,
                ConsumerType = $"Pipeline Producer ({pipeline.ServiceOptionType})"
            });
        }

        return downstreamConsumers;
    }
}
