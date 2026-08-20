using Fdw.Data.DataSets.Abstractions;
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
// DataSetRecord and DataSetSourceConfiguration now in this namespace
// ApiEndpointLog now in this namespace
using Microsoft.Extensions.Logging;
using Fdw.Web.RestEndpoints.Logging;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// Endpoint to get impact analysis for a DataStore or Connection change.
/// </summary>
public abstract class GetImpactAnalysisEndpoint : Endpoint<ImpactAnalysisRequest, ImpactAnalysisResponse>
{
    // Why: IConfigurationGateway routes directly to ConfigurationDb via configurationSchema.json.
    // Using plain IDataGateway would look for "ConfigurationDb" in the runtime DataStore table
    // (data.DataStore), where it does not exist — it is only a bootstrap connection in the JSON.
    private readonly IConfigurationGateway _configurationGateway;
    private readonly ILogger<GetImpactAnalysisEndpoint> _logger;

    /// <inheritdoc />
    protected GetImpactAnalysisEndpoint(IConfigurationGateway configurationGateway, ILogger<GetImpactAnalysisEndpoint> logger)
    {
        _configurationGateway = configurationGateway;
        _logger = logger;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Post("/dataflow/impact");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:write");
#endif
        Summary(s =>
        {
            s.Summary = "Analyze impact of changes";
            s.Description = "Analyzes the impact of changes to a DataStore, Connection, or table on dependent DataSets and consumers.";
        });
    }

    /// <summary>Analyzes the impact of a change to a connection or data store on dependent DataSets.</summary>
    public override async Task HandleAsync(ImpactAnalysisRequest req, CancellationToken ct)
    {
        EndpointLog.GettingResource(_logger, "impact analysis", req.TargetName);

        IReadOnlyList<ImpactedDataSetResponse> impactedDataSets = [];

        if (req.TargetType.Equals("connection", StringComparison.OrdinalIgnoreCase))
        {
            impactedDataSets = await AnalyzeConnectionImpact(req.TargetName, ct).ConfigureAwait(false);
        }
        else if (req.TargetType.Equals("datastore", StringComparison.OrdinalIgnoreCase))
        {
            impactedDataSets = await AnalyzeDataStoreImpact(req.TargetName, ct).ConfigureAwait(false);
        }

        await Send.OkAsync(new ImpactAnalysisResponse
        {
            TargetType = req.TargetType,
            TargetName = req.TargetName,
            ImpactedDataSets = impactedDataSets.ToList(),
            TotalImpactedCount = impactedDataSets.Count,
            HighImpactCount = impactedDataSets.Count(d => string.Equals(d.ImpactLevel, "High", StringComparison.Ordinal)),
            AnalyzedAt = DateTime.UtcNow
        }, ct).ConfigureAwait(false);
    }

    /// <summary>Analyzes the impact of a connection change by finding all sources using that connection.</summary>
    protected virtual async Task<IReadOnlyList<ImpactedDataSetResponse>> AnalyzeConnectionImpact(string connectionName, CancellationToken ct)
    {
        var sources = await FindSourcesByProperty("ConnectionName", connectionName, ct).ConfigureAwait(false);
        return await BuildImpactedDataSets(sources, ct).ConfigureAwait(false);
    }

    /// <summary>Analyzes the impact of a data store change by finding all sources using that data store.</summary>
    protected virtual async Task<IReadOnlyList<ImpactedDataSetResponse>> AnalyzeDataStoreImpact(string dataStoreName, CancellationToken ct)
    {
        var sources = await FindSourcesByProperty("DataStoreName", dataStoreName, ct).ConfigureAwait(false);
        return await BuildImpactedDataSets(sources, ct).ConfigureAwait(false);
    }

    /// <summary>Finds DataSet source records matching a specific property value.</summary>
    protected virtual async Task<IReadOnlyList<DataSetSourceConfiguration>> FindSourcesByProperty(string propertyName, string value, CancellationToken ct)
    {
        // Why: Addressing moved off IDataCommand onto DataStoreTarget.
        var command = new QueryCommand<DataSetSourceConfiguration>
        {
            Filter = new FilterExpression
            {
                Root = new FilterCondition
                {
                    PropertyName = propertyName,
                    Operator = FilterOperators.ByName("Equal"),
                    Value = value
                }
            }
        };

        var result = await _configurationGateway.Execute<IEnumerable<DataSetSourceConfiguration>>(
            command, new DataStoreTarget("ConfigurationDb", "data", "DataSetSource"), ct).ConfigureAwait(false);
        return result.IsSuccess ? result.Value?.ToList() ?? [] : [];
    }

    /// <summary>Groups sources by DataSet and builds impact assessment DTOs with impact level classification.</summary>
    protected virtual async Task<IReadOnlyList<ImpactedDataSetResponse>> BuildImpactedDataSets(IReadOnlyList<DataSetSourceConfiguration> sources, CancellationToken ct)
    {
        var impacted = new List<ImpactedDataSetResponse>();
        var dataSetIds = sources.Select(s => s.DataSetId).Distinct();

        foreach (var dsId in dataSetIds)
        {
            // Why: Addressing moved off IDataCommand onto DataStoreTarget.
            var dsCommand = new QueryCommand<DataSetRecord>
            {
                Filter = new FilterExpression
                {
                    Root = new FilterCondition
                    {
                        PropertyName = "Id",
                        Operator = FilterOperators.ByName("Equal"),
                        Value = dsId
                    }
                }
            };

            var dsResult = await _configurationGateway.Execute<IEnumerable<DataSetRecord>>(
                dsCommand, new DataStoreTarget("ConfigurationDb", "data", "DataSet"), ct).ConfigureAwait(false);
            var ds = dsResult.IsSuccess ? dsResult.Value?.FirstOrDefault() : null;

            if (ds != null)
            {
                var affectedSources = sources.Where(s => s.DataSetId == dsId).ToList();
                impacted.Add(new ImpactedDataSetResponse
                {
                    DataSetName = ds.Name,
                    Category = ds.Category,
                    ImpactLevel = affectedSources.Any(s => s.Priority == 1) ? "High" : "Medium",
                    AffectedSourceCount = affectedSources.Count,
                    AffectedSources = affectedSources.Select(s => s.SourceName).ToList()
                });
            }
        }

        return impacted;
    }
}
