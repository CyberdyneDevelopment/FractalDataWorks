using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>
/// Generic base endpoint for retrieving a specific schedule configuration by name.
/// </summary>
/// <typeparam name="TConfig">The concrete schedule configuration type.</typeparam>
public abstract class GetScheduleEndpointBase<TConfig> : CrudGetEndpointBase<ScheduleNameRequest, ScheduleDetailDto>
    where TConfig : ScheduleConfiguration
{
    // Why: ScheduleConfigurationProvider replaces IOptionsMonitor<List<T>> with dual-source
    // (ctrl + cfg) provider that merges system and user configurations.
    private readonly ScheduleConfigurationProvider _provider;

    /// <inheritdoc />
    protected GetScheduleEndpointBase(ScheduleConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "schedules";

    /// <summary>Returns the schedule name as the resource identifier.</summary>
    protected override string GetResourceIdentifier(ScheduleNameRequest request) => request.Name;

    /// <summary>Finds a schedule by name and maps it to a detail DTO.</summary>
    protected override async Task<IGenericResult<ScheduleDetailDto?>> FindByIdentifier(ScheduleNameRequest request, CancellationToken ct)
    {
        var scheduleResult = await _provider.Get(request.Name, ct).ConfigureAwait(false);

        ScheduleDetailDto? detail = scheduleResult.IsSuccess && scheduleResult.Value != null
            ? MapToDetail((TConfig)scheduleResult.Value) : null;
        return GenericResult<ScheduleDetailDto?>.Success(detail);
    }

    /// <summary>Maps a concrete schedule configuration to a detail DTO. Override for type-specific fields.</summary>
    protected abstract ScheduleDetailDto MapToDetail(TConfig config);
}
