using Fdw.Data.DataSets;
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
using Fdw.Services.Data;
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
public abstract class GetImpactAnalysisEndpointBase : Endpoint<ImpactAnalysisRequest, ImpactAnalysisResponse>
{
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly ILogger<GetImpactAnalysisEndpointBase> _logger;

    /// <inheritdoc />
    protected GetImpactAnalysisEndpointBase(DataSetConfigurationProvider dataSetProvider, ILogger<GetImpactAnalysisEndpointBase> logger)
    {
        _dataSetProvider = dataSetProvider;
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
    protected virtual Task<IReadOnlyList<ImpactedDataSetResponse>> AnalyzeConnectionImpact(
        string connectionName, CancellationToken ct) =>
        Impact(source => string.Equals(source.ConnectionName, connectionName, StringComparison.OrdinalIgnoreCase), ct);

    /// <summary>Analyzes the impact of a data store change by finding all sources using that data store.</summary>
    protected virtual Task<IReadOnlyList<ImpactedDataSetResponse>> AnalyzeDataStoreImpact(
        string dataStoreName, CancellationToken ct) =>
        Impact(source => string.Equals(source.DataStoreName, dataStoreName, StringComparison.OrdinalIgnoreCase), ct);

    /// <summary>Builds impact assessments for every DataSet with a source matching <paramref name="matches"/>.</summary>
    /// <remarks>
    /// One read. The provider composes each DataSet with its sources, so the DataSets are already in
    /// hand once the sources are - where this previously filtered sources in one query and then
    /// issued another query per distinct DataSet id.
    /// <para>
    /// The match is a predicate rather than a property name and value. The old form passed
    /// "ConnectionName" as a string into a filter, so a typo produced an empty result rather than a
    /// compile error, and only two properties were ever passed.
    /// </para>
    /// </remarks>
    protected virtual async Task<IReadOnlyList<ImpactedDataSetResponse>> Impact(
        Func<DataSetSourceConfiguration, bool> matches, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(matches);

        var dataSets = await _dataSetProvider.Get(ct).ConfigureAwait(false);
        if (dataSets.IsFailure || dataSets.Value is not { } all)
            return [];

        var impacted = new List<ImpactedDataSetResponse>();

        foreach (var dataSet in all)
        {
            var affectedSources = (dataSet.Sources ?? []).Where(matches).ToList();
            if (affectedSources.Count == 0)
                continue;

            impacted.Add(new ImpactedDataSetResponse
            {
                DataSetName = dataSet.Name,
                Category = dataSet.Category,
                ImpactLevel = affectedSources.Exists(s => s.Priority == 1) ? "High" : "Medium",
                AffectedSourceCount = affectedSources.Count,
                AffectedSources = affectedSources.ConvertAll(s => s.SourceName)
            });
        }

        return impacted;
    }
}
