using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>
/// Generic base endpoint for creating a new schedule configuration.
/// </summary>
/// <typeparam name="TConfig">The concrete schedule configuration type.</typeparam>
public abstract class CreateScheduleEndpointBase<TConfig> : CrudCreateEndpointBase<CreateScheduleRequest, ScheduleDetailDto>
    where TConfig : ScheduleConfiguration
{
    private readonly ScheduleConfigurationProvider _provider;

    /// <inheritdoc />
    protected CreateScheduleEndpointBase(ScheduleConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "schedules";

    /// <summary>Returns the schedule name from the create request.</summary>
    protected override string GetResourceName(CreateScheduleRequest request) => request.Name;

    /// <summary>Checks whether a schedule with the requested name already exists.</summary>
    protected override async Task<IGenericResult<bool>> CheckExists(CreateScheduleRequest request, CancellationToken ct)
    {
        var existingResult = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        return GenericResult<bool>.Success(existingResult.IsSuccess && existingResult.Value != null);
    }

    /// <summary>Creates the schedule configuration and persists it via the DataGateway.</summary>
    protected override async Task<IGenericResult<ScheduleDetailDto>> Create(CreateScheduleRequest request, CancellationToken ct)
    {
        var scheduleId = Guid.CreateVersion7();
        var config = CreateConfiguration(request, scheduleId);

        var saveResult = await _provider.Save(config, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            return saveResult.ToNewResult<ScheduleDetailDto>();
        }

        return GenericResult<ScheduleDetailDto>.Success(MapToDetail(config, request, scheduleId));
    }

    /// <summary>Builds a concrete schedule configuration from the create request. Override for type-specific fields.</summary>
    protected abstract TConfig CreateConfiguration(CreateScheduleRequest request, Guid scheduleId);

    /// <summary>Maps the saved configuration to a detail DTO. Override for type-specific fields.</summary>
    protected abstract ScheduleDetailDto MapToDetail(TConfig savedConfig, CreateScheduleRequest request, Guid scheduleId);

    /// <summary>Sends a 201 Created response with the schedule detail.</summary>
    protected override Task SendCreatedResponse(ScheduleDetailDto detail, CancellationToken ct)
    {
        return Send.ResponseAsync(detail, 201, ct);
    }
}
