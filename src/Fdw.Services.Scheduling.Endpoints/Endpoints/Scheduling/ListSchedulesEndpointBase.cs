using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;
using Fdw.Web.RestEndpoints.Models;
using Fdw.Web.RestEndpoints.Pagination;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>
/// Generic base endpoint for listing all configured schedules with pagination and filtering.
/// Uses the base <see cref="IScheduleImplementationConfiguration"/> type, which covers all schedule types.
/// </summary>
public abstract class ListSchedulesEndpointBase : CrudListEndpointBase<ListSchedulesRequest, ScheduleSummaryDto>
{
    private readonly IScheduleConfigurationProvider _provider;

    /// <inheritdoc />
    protected ListSchedulesEndpointBase(ILogger<ListSchedulesEndpointBase> logger, IScheduleConfigurationProvider provider) : base(logger)
    {
        _provider = provider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "schedules";

    /// <summary>Loads all schedule configurations and maps them to summary DTOs.</summary>
    protected override async Task<IGenericResult<List<ScheduleSummaryDto>>> LoadItems(ListSchedulesRequest request, CancellationToken ct)
    {
        // Every schedule, read as deep as the summary needs (ScheduleDtoMapper.ReadLevels): a row shows its run parameters and run
        // roles, which are child collections, and a collection below the depth read comes back empty, not absent (FDW-1168).
        // The filters and sort run on implementation columns the paged read cannot filter on, so
        // this list is filtered and paged here rather than by the provider (FDW-951).
        var allResult = await _provider.Get(ScheduleDtoMapper.ReadLevels, ct).ConfigureAwait(false);
        if (!allResult.IsSuccess || allResult.Value is null)
        {
            return allResult.ToNewResult<List<ScheduleSummaryDto>>();
        }

        var allSchedules = allResult.Value
            .Where(config => !string.IsNullOrWhiteSpace(config.Name))
            .ToList();

        var filtered = ApplyFilters(allSchedules, request);
        var sorted = ApplySort(filtered, request);

        var items = sorted
            .Select(MapToSummary)
            .ToList();

        return GenericResult<List<ScheduleSummaryDto>>.Success(items);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Why Page/PageSize rather than skip/take: they are the fields <see cref="ListSchedulesRequest"/>
    /// declares, so they are the ones a caller reading the contract sends. They were bound and logged
    /// but never applied, so every call returned every schedule.
    /// </remarks>
    protected override IGenericResult<PaginatedListRequest> ResolvePaging(ListSchedulesRequest request)
        => PaginationQuery.ResolvePage(HttpContext, Logger, request.Page, request.PageSize);

    /// <summary>Applies filters from the request to the schedule list.</summary>
    protected virtual IReadOnlyList<IScheduleImplementationConfiguration> ApplyFilters(IReadOnlyList<IScheduleImplementationConfiguration> schedules, ListSchedulesRequest request)
    {
        var filtered = schedules.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.PipelineName))
        {
            filtered = filtered.Where(s => string.Equals(s.PipelineName, request.PipelineName, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.SchedulerType))
        {
            filtered = filtered.Where(s => string.Equals(s.ScheduleType, request.SchedulerType, StringComparison.OrdinalIgnoreCase));
        }

        if (request.IsEnabled.HasValue)
        {
            filtered = filtered.Where(s => s.IsEnabled == request.IsEnabled.Value);
        }

        return filtered.ToList();
    }

    /// <summary>Applies sorting from the request to the schedule list.</summary>
    protected virtual IEnumerable<IScheduleImplementationConfiguration> ApplySort(IReadOnlyList<IScheduleImplementationConfiguration> schedules, ListSchedulesRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SortBy))
        {
            return schedules.OrderBy(s => s.Name, StringComparer.Ordinal);
        }

        var ordered = request.SortBy.ToLowerInvariant() switch
        {
            "name" => request.SortDescending
                ? schedules.OrderByDescending(s => s.Name, StringComparer.Ordinal)
                : schedules.OrderBy(s => s.Name, StringComparer.Ordinal),
            "pipelinename" => request.SortDescending
                ? schedules.OrderByDescending(s => s.PipelineName, StringComparer.Ordinal)
                : schedules.OrderBy(s => s.PipelineName, StringComparer.Ordinal),
            "nextruntime" => request.SortDescending
                ? schedules.OrderByDescending(s => s.NextRunTime)
                : schedules.OrderBy(s => s.NextRunTime),
            _ => schedules.OrderBy(s => s.Name, StringComparer.Ordinal)
        };

        return ordered;
    }

    /// <summary>Maps a single schedule configuration to a summary DTO.</summary>
    protected virtual ScheduleSummaryDto MapToSummary(IScheduleImplementationConfiguration config) => ScheduleDtoMapper.Summary(config);
}
