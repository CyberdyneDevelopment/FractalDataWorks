using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Abstractions.Models;
using Fdw.Services.Scheduling.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>
/// Generic base endpoint for updating an existing schedule configuration.
/// </summary>
/// <typeparam name="TConfig">The concrete schedule configuration type.</typeparam>
public abstract class UpdateScheduleEndpointBase<TConfig> : CrudUpdateEndpointBase<UpdateScheduleRequest, ScheduleDetailDto>
    where TConfig : class, IScheduleImplementationConfiguration
{
    private readonly ScheduleConfigurationProvider _provider;
    private readonly ScheduleRunRoleAuthority _runRoles;

    /// <inheritdoc />
    protected UpdateScheduleEndpointBase(
        ILogger<UpdateScheduleEndpointBase<TConfig>> logger,
        ScheduleConfigurationProvider provider,
        IRoleGrants grants) : base(logger)
    {
        _provider = provider;
        _runRoles = new ScheduleRunRoleAuthority(grants);
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

        // A scheduler type other than the stored one is refused, not accepted and ignored (FDW-1171): the type is the implementation
        // the row is saved under, so an update cannot change it. One the body does not state, or states as the stored one, is not judged.
        var type = UpdateScheduleChanges.CheckType(request, originalResult.Value.Implementation, Logger);
        if (type.IsFailure)
        {
            return type.ToNewResult<ScheduleDetailDto>();
        }

        // A zone the server cannot resolve is refused before anything is stored (FDW-1170); one the body does not state is not judged.
        var zone = ScheduleTimeZone.Check(request.TimeZoneId, Logger);
        if (zone.IsFailure)
        {
            return zone.ToNewResult<ScheduleDetailDto>();
        }

        // A request that names roles replaces the recorded ones, and only with roles the caller holds (FDW-1161); one that
        // names none leaves them standing.
        IReadOnlyList<string>? roles = null;
        if (request.RunRoles is not null)
        {
            var authorized = await _runRoles.Authorize(
                new ClaimsPrincipalAuthenticationContext(User), originalResult.Value.PipelineName, request.RunRoles, Logger, ct).ConfigureAwait(false);
            if (authorized.IsFailure)
            {
                return authorized.ToNewResult<ScheduleDetailDto>();
            }

            roles = authorized.Value;
        }

        var updatedConfig = MergeUpdate(request, (TConfig)originalResult.Value, roles);

        var saveResult = await _provider.Save(updatedConfig, "Schedule", updatedConfig.Implementation, updatedConfig.Name, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            return saveResult.ToNewResult<ScheduleDetailDto>();
        }

        return GenericResult<ScheduleDetailDto>.Success(MapUpdatedToDetail(updatedConfig, updatedConfig));
    }

    /// <summary>Maps an existing configuration to a detail DTO for the find phase. Override for type-specific fields.</summary>
    protected abstract ScheduleDetailDto MapExistingToDetail(TConfig config);

    /// <summary>Merges the update request into the existing configuration. Override for type-specific fields.</summary>
    protected abstract TConfig MergeUpdate(UpdateScheduleRequest request, TConfig existing, IReadOnlyList<string>? runRoles);

    /// <summary>Maps the saved updated configuration to a detail DTO. Override for type-specific fields.</summary>
    protected abstract ScheduleDetailDto MapUpdatedToDetail(TConfig savedConfig, TConfig updatedConfig);
}
