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
using Fdw.Services.Data;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// Endpoint to get impact analysis for a DataStore or Connection change.
/// </summary>
public abstract class GetImpactAnalysisEndpointBase : Endpoint<ImpactAnalysisRequest, ImpactAnalysisResponse>
{
    private readonly DataSetConfigurationProvider _dataSets;
    private readonly ILogger<GetImpactAnalysisEndpointBase> _logger;

    /// <inheritdoc />
    protected GetImpactAnalysisEndpointBase(DataSetConfigurationProvider dataSets, ILogger<GetImpactAnalysisEndpointBase> logger)
    {
        _dataSets = dataSets;
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

    /// <summary>Finds the sources whose <paramref name="propertyName"/> equals <paramref name="value"/>.</summary>
    /// <param name="propertyName">The source property to match on.</param>
    /// <param name="value">The value to match.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <remarks>
    /// Read through the DataSet provider rather than queried per container: the provider's read
    /// cascade already composes Sources onto each DataSet, so one call returns the whole graph this
    /// walks. Matching in memory also replaces a query-per-DataSet with a single read.
    /// </remarks>
    protected virtual async Task<IReadOnlyList<DataSetSourceConfiguration>> FindSourcesByProperty(
        string propertyName, string value, CancellationToken cancellationToken)
    {
        var dataSets = await _dataSets.Get(cancellationToken).ConfigureAwait(false);
        if (dataSets.IsFailure)
            return [];

        return (dataSets.Value ?? [])
            .SelectMany(d => d.Sources ?? [])
            .Where(source => string.Equals(SourceProperty(source, propertyName), value, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>Reads the named property off a source.</summary>
    /// <param name="source">The source to read.</param>
    /// <param name="propertyName">The property to read.</param>
    /// <returns>The value, or null when the source does not carry that property.</returns>
    private static string? SourceProperty(DataSetSourceConfiguration source, string propertyName)
        => propertyName switch
        {
            "ConnectionName" => source.ConnectionName,
            "DataStoreName" => source.DataStoreName,
            _ => null,
        };

    /// <summary>Groups sources by DataSet and builds impact assessment DTOs with impact level classification.</summary>
    protected virtual async Task<IReadOnlyList<ImpactedDataSetResponse>> BuildImpactedDataSets(IReadOnlyList<DataSetSourceConfiguration> sources, CancellationToken cancellationToken)
    {
        var impacted = new List<ImpactedDataSetResponse>();
        var dataSetIds = sources.Select(s => s.DataSetId).Distinct();

        foreach (var dsId in dataSetIds)
        {
            var dsResult = await _dataSets.Get(dsId, cancellationToken).ConfigureAwait(false);
            var ds = dsResult.IsSuccess ? dsResult.Value : null;

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
