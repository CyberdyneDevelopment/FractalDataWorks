using Fdw.Services.Etl.Execution;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Abstractions.Models;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>
/// Generic base endpoint for creating a new schedule configuration.
/// </summary>
/// <remarks>
/// The pipeline arrives as an id the API proxy resolved in its caller's view (FDW-1088). The scheduler has no
/// tenant of its own to look it up in, so it does not look again: it stores the id it was given, and the ETL
/// resolves that id when the schedule fires.
/// </remarks>
/// <typeparam name="TConfig">The concrete schedule configuration type.</typeparam>
public abstract class CreateScheduleEndpointBase<TConfig> : CrudCreateEndpointBase<CreateScheduleRequest, ScheduleDetailDto>
    where TConfig : class, IScheduleImplementationConfiguration
{
    private readonly ScheduleConfigurationProvider _provider;
    private readonly RunRoleAuthority _runRoles;

    /// <inheritdoc />
    protected CreateScheduleEndpointBase(
        ILogger<CreateScheduleEndpointBase<TConfig>> logger,
        ScheduleConfigurationProvider provider,
        IRoleGrants grants) : base(logger)
    {
        _provider = provider;
        _runRoles = new RunRoleAuthority(grants);
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
        // The zone the schedule is evaluated in must be stated and one the server can resolve (FDW-1170, FDW-2059); a missing or bad one is refused before anything is stored.
        var zone = ScheduleTimeZone.Require(request.TimeZoneId, Logger);
        if (zone.IsFailure)
        {
            return zone.ToNewResult<ScheduleDetailDto>();
        }

        // The roles every fire acts with are the creator's choice (FDW-1161), and only roles the creator holds.
        var roles = await _runRoles.Authorize(
            new ClaimsPrincipalAuthenticationContext(User), request.PipelineName, request.RunRoles, Logger, ct).ConfigureAwait(false);
        if (roles.IsFailure)
        {
            return roles.ToNewResult<ScheduleDetailDto>();
        }

        var scheduleId = Guid.CreateVersion7();
        var config = CreateConfiguration(request, scheduleId, roles.Value!);

        var saveResult = await _provider.Save(config, "Schedule", config.Implementation, config.Name, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            return saveResult.ToNewResult<ScheduleDetailDto>();
        }

        return GenericResult<ScheduleDetailDto>.Success(MapToDetail(config, request, scheduleId));
    }

    /// <summary>Builds a concrete schedule configuration from the create request. Override for type-specific fields.</summary>
    protected abstract TConfig CreateConfiguration(CreateScheduleRequest request, Guid scheduleId, IReadOnlyList<string> runRoles);

    /// <summary>Maps the saved configuration to a detail DTO. Override for type-specific fields.</summary>
    protected abstract ScheduleDetailDto MapToDetail(TConfig savedConfig, CreateScheduleRequest request, Guid scheduleId);

    /// <summary>Sends a 201 Created response with the schedule detail.</summary>
    protected override Task SendCreatedResponse(ScheduleDetailDto detail, CancellationToken ct)
    {
        return Send.ResponseAsync(detail, 201, ct);
    }
}
