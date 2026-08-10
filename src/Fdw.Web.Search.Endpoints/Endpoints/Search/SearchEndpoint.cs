using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
// ApiEndpointLog now in this namespace
using Microsoft.Extensions.Logging;
using Fdw.Web.Search.Clients.Models;
using Fdw.Web.RestEndpoints.Logging;

namespace Fdw.Web.Search.Endpoints;

/// <summary>
/// Endpoint for global search across entities.
/// </summary>
public abstract class SearchEndpoint : Endpoint<SearchRequest, SearchResponse>
{
    private readonly IDataGateway _dataGateway;
    // Why: ConnectionConfigurationProvider (dual-source) replaces IConnectionProvider.GetAllConnectionConfigurations()
    // which was removed. The provider merges system (ctrl) and user (cfg) connection configs.
    private readonly ConnectionConfigurationProvider _configProvider;
    private readonly ILogger<SearchEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchEndpoint"/> class.
    /// </summary>
    protected SearchEndpoint(
        IDataGateway dataGateway,
        ConnectionConfigurationProvider configProvider,
        ILogger<SearchEndpoint> logger)
    {
        _dataGateway = dataGateway;
        _configProvider = configProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/search");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("datastores:read");
#endif
        Summary(s =>
        {
            s.Summary = "Global search";
            s.Description = "Searches across pipelines, connections, datasets, and schedules. Supports wildcard matching.";
            s.ExampleRequest = new SearchRequest { Query = "Nfl", Types = ["pipelines", "connections"], Limit = 10 };
        });
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(SearchRequest req, CancellationToken ct)
    {
        var startTime = DateTime.UtcNow;
        EndpointLog.ListingResources(_logger, "search results");

        // Why: q= is mandatory — empty/missing query returns 400 instead of an empty 200.
        if (string.IsNullOrWhiteSpace(req.Query))
        {
            AddError("Query parameter 'q' is required");
            await Send.ErrorsAsync(cancellation: ct).ConfigureAwait(false);
            return;
        }

        var response = new SearchResponse
        {
            Query = req.Query
        };

        var limit = Math.Min(Math.Max(req.Limit, 1), 100);
        var searchTypes = req.Types ?? ["pipelines", "connections", "datasets", "schedules"];

        var tasks = new List<Task<IList<SearchResultPayload>>>();

        if (searchTypes.Contains("pipelines", StringComparer.OrdinalIgnoreCase))
        {
            tasks.Add(SearchPipelines(req.Query, limit, ct));
        }

        if (searchTypes.Contains("connections", StringComparer.OrdinalIgnoreCase))
        {
            tasks.Add(SearchConnections(req.Query, limit, ct));
        }

        if (searchTypes.Contains("datasets", StringComparer.OrdinalIgnoreCase))
        {
            tasks.Add(SearchDataSets(req.Query, limit, ct));
        }

        if (searchTypes.Contains("schedules", StringComparer.OrdinalIgnoreCase))
        {
            tasks.Add(SearchSchedules(req.Query, limit, ct));
        }

        IList<SearchResultPayload>[] results;
        try
        {
            results = await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Why: Return 500 rather than partial search results — the caller cannot tell data is incomplete.
            EndpointLog.OperationFailed(_logger, ex, "search", "all", req.Query);
            AddError("An error occurred while executing the search");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        var allResults = results.SelectMany(r => r).ToList();

        foreach (var group in allResults.GroupBy(r => r.Type, StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            response.Facets[group.Key] = group.Count();
        }

        response.TotalCount = allResults.Count;
        response.Results = allResults.Take(limit).ToList();
        response.DurationMs = (DateTime.UtcNow - startTime).TotalMilliseconds;

        await Send.OkAsync(response, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Searches pipelines by name, description, or pipeline type.
    /// </summary>
    protected virtual async Task<IList<SearchResultPayload>> SearchPipelines(string query, int limit, CancellationToken ct)
    {
        var results = new List<SearchResultPayload>();

        try
        {
            var command = DataQuery.From<SearchablePipelineRecord>("ConfigurationDb", "pipe", "Pipeline").Build();

            var dbResult = await _dataGateway.Execute<IEnumerable<SearchablePipelineRecord>>(command, ct).ConfigureAwait(false);
            if (dbResult.IsSuccess && dbResult.Value != null)
            {
                var dbMatches = dbResult.Value
                    .Where(p => MatchesQuery(p.Name, query) || MatchesQuery(p.Description, query) || MatchesQuery(p.PipelineType, query))
                    .Take(limit)
                    .Select(p => new SearchResultPayload
                    {
                        Type = "Pipeline",
                        Name = p.Name,
                        Description = p.Description,
                        MatchedField = MatchesQuery(p.Name, query) ? "Name" : MatchesQuery(p.PipelineType, query) ? "PipelineType" : "Description",
                        Url = $"/pipelines/builder/{p.Id}",
                        Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
                        {
                            ["PipelineType"] = p.PipelineType
                        }
                    });

                results.AddRange(dbMatches);
            }
        }
        catch (Exception ex)
        {
            // Why: Re-throw so Task.WhenAll propagates the error to HandleAsync → 500.
            EndpointLog.OperationFailed(_logger, ex, "search", "pipelines", query);
            throw;
        }

        return results.Take(limit).ToList();
    }

    /// <summary>
    /// Searches connections by name or service option type.
    /// </summary>
    protected virtual async Task<IList<SearchResultPayload>> SearchConnections(string query, int limit, CancellationToken ct)
    {
        var allResult = await _configProvider.Get(ct).ConfigureAwait(false);
        if (!allResult.IsSuccess || allResult.Value is null)
        {
            return [];
        }

        var results = allResult.Value
            .Where(c => MatchesQuery(c.Name, query) || MatchesQuery(c.ConnectionType, query))
            .Take(limit)
            .Select(c => new SearchResultPayload
            {
                Type = "Connection",
                Name = c.Name,
                Description = c.Description,
                MatchedField = MatchesQuery(c.Name, query) ? "Name" : "Type",
                Url = $"/connections/{c.Name}",
                Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["ConnectionType"] = c.ConnectionType ?? "Unknown"
                }
            })
            .ToList();

        return results;
    }

    /// <summary>
    /// Searches datasets by name or description.
    /// </summary>
    protected virtual async Task<IList<SearchResultPayload>> SearchDataSets(string query, int limit, CancellationToken ct)
    {
        var results = new List<SearchResultPayload>();

        try
        {
            var command = DataQuery.From<SearchableDataSetRecord>("ConfigurationDb", "data", "DataSet").Build();

            var dbResult = await _dataGateway.Execute<IEnumerable<SearchableDataSetRecord>>(command, ct).ConfigureAwait(false);
            if (dbResult.IsSuccess && dbResult.Value != null)
            {
                results = dbResult.Value
                    .Where(d => MatchesQuery(d.Name, query) || MatchesQuery(d.Description, query))
                    .Take(limit)
                    .Select(d => new SearchResultPayload
                    {
                        Type = "DataSet",
                        Name = d.Name,
                        Description = d.Description,
                        MatchedField = MatchesQuery(d.Name, query) ? "Name" : "Description",
                        Url = $"/datasets/{d.Name}",
                        Metadata = d.ConnectionName != null
                            ? new Dictionary<string, string>(StringComparer.Ordinal) { ["ConnectionName"] = d.ConnectionName }
                            : new Dictionary<string, string>(StringComparer.Ordinal)
                    })
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            // Why: Re-throw so Task.WhenAll propagates the error to HandleAsync → 500.
            EndpointLog.OperationFailed(_logger, ex, "search", "datasets", query);
            throw;
        }

        return results;
    }

    /// <summary>
    /// Searches schedules by name or pipeline name.
    /// </summary>
    protected virtual async Task<IList<SearchResultPayload>> SearchSchedules(string query, int limit, CancellationToken ct)
    {
        var results = new List<SearchResultPayload>();

        try
        {
            var command = DataQuery.From<SearchableScheduleRecord>("ConfigurationDb", "sched", "Schedule")
                .Where("IsDeleted", false)
                .Build();

            var dbResult = await _dataGateway.Execute<IEnumerable<SearchableScheduleRecord>>(command, ct).ConfigureAwait(false);
            if (dbResult.IsSuccess && dbResult.Value != null)
            {
                results = dbResult.Value
                    .Where(s => MatchesQuery(s.Name, query) || MatchesQuery(s.PipelineName, query))
                    .Take(limit)
                    .Select(s => new SearchResultPayload
                    {
                        Type = "Schedule",
                        Name = s.Name,
                        Description = s.PipelineName != null ? $"Runs {s.PipelineName}" : null,
                        MatchedField = MatchesQuery(s.Name, query) ? "Name" : "PipelineName",
                        Url = $"/schedules/{s.Name}",
                        Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
                        {
                            ["SchedulerType"] = s.SchedulerType ?? "Manual",
                            ["PipelineName"] = s.PipelineName ?? string.Empty
                        }
                    })
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            // Why: Re-throw so Task.WhenAll propagates the error to HandleAsync → 500.
            EndpointLog.OperationFailed(_logger, ex, "search", "schedules", query);
            throw;
        }

        return results;
    }

    /// <summary>
    /// Determines whether a value matches the search query, supporting wildcard patterns.
    /// </summary>
    protected static bool MatchesQuery(string? value, string query)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(query))
            return false;

        if (query.Contains('*'))
        {
            var pattern = query.Replace("*", "");
            if (query.StartsWith('*') && query.EndsWith('*'))
                return value.Contains(pattern, StringComparison.OrdinalIgnoreCase);
            if (query.StartsWith('*'))
                return value.EndsWith(pattern, StringComparison.OrdinalIgnoreCase);
            if (query.EndsWith('*'))
                return value.StartsWith(pattern, StringComparison.OrdinalIgnoreCase);
        }

        return value.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}
