using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>
/// Generic base endpoint for updating an existing schedule configuration.
/// </summary>
/// <typeparam name="TConfig">The concrete schedule configuration type.</typeparam>
public abstract class UpdateScheduleEndpointBase<TConfig> : CrudUpdateEndpointBase<UpdateScheduleRequest, ScheduleDetailDto>
    where TConfig : ScheduleConfiguration
{
    private readonly ScheduleConfigurationProvider _provider;

    /// <inheritdoc />
    protected UpdateScheduleEndpointBase(ScheduleConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "schedules";

    /// <summary>Returns the schedule name as the resource identifier.</summary>
    protected override string GetResourceIdentifier(UpdateScheduleRequest request) => request.Name;

    /// <summary>Finds the existing schedule to update, returning null if not found.</summary>
    protected override async Task<IGenericResult<ScheduleDetailDto?>> FindForUpdate(UpdateScheduleRequest request, CancellationToken ct)
    {
        var existingResult = await _provider.Get(request.Name, ct).ConfigureAwait(false);

        if (!existingResult.IsSuccess || existingResult.Value == null)
        {
            return GenericResult<ScheduleDetailDto?>.Success((ScheduleDetailDto?)null);
        }

        var detail = MapExistingToDetail((TConfig)existingResult.Value);
        return GenericResult<ScheduleDetailDto?>.Success((ScheduleDetailDto?)detail);
    }

    /// <summary>Merges the update request with the existing configuration and persists the changes via the DataGateway.</summary>
    protected override async Task<IGenericResult<ScheduleDetailDto>> Update(UpdateScheduleRequest request, ScheduleDetailDto existing, CancellationToken ct)
    {
        var originalResult = await _provider.Get(request.Name, ct).ConfigureAwait(false);

        if (!originalResult.IsSuccess || originalResult.Value is null)
        {
            return GenericResult<ScheduleDetailDto>.Failure(
                ScheduleEndpointLog.ScheduleNotFound(Logger, request.Name));
        }

        var updatedConfig = MergeUpdate(request, (TConfig)originalResult.Value);

        var saveResult = await _provider.Save(updatedConfig, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            return saveResult.ToNewResult<ScheduleDetailDto>();
        }

        return GenericResult<ScheduleDetailDto>.Success(MapUpdatedToDetail(updatedConfig, updatedConfig));
    }

    /// <summary>Maps an existing configuration to a detail DTO for the find phase. Override for type-specific fields.</summary>
    protected abstract ScheduleDetailDto MapExistingToDetail(TConfig config);

    /// <summary>Merges the update request into the existing configuration. Override for type-specific fields.</summary>
    protected abstract TConfig MergeUpdate(UpdateScheduleRequest request, TConfig existing);

    /// <summary>Maps the saved updated configuration to a detail DTO. Override for type-specific fields.</summary>
    protected abstract ScheduleDetailDto MapUpdatedToDetail(TConfig savedConfig, TConfig updatedConfig);
}
