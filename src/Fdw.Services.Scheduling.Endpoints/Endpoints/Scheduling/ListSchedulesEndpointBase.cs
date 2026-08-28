using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>
/// Generic base endpoint for listing all configured schedules with pagination and filtering.
/// Uses the base <see cref="ScheduleConfiguration"/> type, which covers all schedule types.
/// </summary>
public abstract class ListSchedulesEndpointBase : CrudListEndpointBase<ListSchedulesRequest, ScheduleSummaryDto>
{
    private readonly IServiceConfigurationProvider<ScheduleConfiguration> _provider;

    /// <inheritdoc />
    protected ListSchedulesEndpointBase(IServiceConfigurationProvider<ScheduleConfiguration> provider)
    {
        _provider = provider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "schedules";

    /// <summary>Loads all schedule configurations and maps them to summary DTOs.</summary>
    protected override async Task<IGenericResult<List<ScheduleSummaryDto>>> LoadItems(ListSchedulesRequest request, CancellationToken ct)
    {
        var allResult = await _provider.Get(ct).ConfigureAwait(false);
        if (!allResult.IsSuccess)
        {
            return allResult.ToNewResult<List<ScheduleSummaryDto>>();
        }

        var allSchedules = (allResult.Value ?? (IReadOnlyList<ScheduleConfiguration>)[])
            .Where(config => !string.IsNullOrWhiteSpace(config.Name))
            .ToList();

        var filtered = ApplyFilters(allSchedules, request);
        var sorted = ApplySort(filtered, request);

        var items = sorted
            .Select(MapToSummary)
            .ToList();

        return GenericResult<List<ScheduleSummaryDto>>.Success(items);
    }

    /// <summary>Applies filters from the request to the schedule list.</summary>
    protected virtual IReadOnlyList<ScheduleConfiguration> ApplyFilters(IReadOnlyList<ScheduleConfiguration> schedules, ListSchedulesRequest request)
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
    protected virtual IEnumerable<ScheduleConfiguration> ApplySort(IReadOnlyList<ScheduleConfiguration> schedules, ListSchedulesRequest request)
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
    protected virtual ScheduleSummaryDto MapToSummary(ScheduleConfiguration config)
    {
        return new ScheduleSummaryDto
        {
            Id = config.Id,
            Name = config.Name,
            PipelineName = config.PipelineName,
            SchedulerType = config.ScheduleType,
            IsEnabled = config.IsEnabled,
            NextRunTime = config.NextRunTime,
            CreatedAt = config.CreateDate,
            CreatedBy = config.CreateBy,
            ModifiedBy = config.ModifyBy,
            CreatedOnBehalfOf = config.CreateOnBehalfOf,
            ModifiedOnBehalfOf = config.ModifyOnBehalfOf
        };
    }
}
