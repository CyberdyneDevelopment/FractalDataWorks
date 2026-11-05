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
using Fdw.Services.Settings;
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
    private readonly Lazy<IEffectiveSettingsProvider> _settings;

    /// <inheritdoc />
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The schedule configuration provider.</param>
    /// <param name="grants">The role grants the run roles are checked against.</param>
    /// <param name="settings">The settings that hold the defaults of what a create request leaves out (FDW-3580).</param>
    protected CreateScheduleEndpointBase(
        ILogger<CreateScheduleEndpointBase<TConfig>> logger,
        ScheduleConfigurationProvider provider,
        IRoleGrants grants,
        Lazy<IEffectiveSettingsProvider> settings) : base(logger)
    {
        _provider = provider;
        _runRoles = new RunRoleAuthority(grants);
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
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
        var caller = new ClaimsPrincipalAuthenticationContext(User);
        var roles = await _runRoles.Authorize(caller, request.PipelineName, request.RunRoles, Logger, ct).ConfigureAwait(false);
        if (roles.IsFailure)
        {
            return roles.ToNewResult<ScheduleDetailDto>();
        }

        var scheduleId = Guid.CreateVersion7();
        var config = CreateConfiguration(request, scheduleId, roles.Value!);
        var defaults = ApplyDefaults(config, request, caller.ActiveTenantId);
        if (defaults.IsFailure)
        {
            return defaults.ToNewResult<ScheduleDetailDto>();
        }

        var saveResult = await _provider.Save(config, "Schedule", config.Implementation, config.Name, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            return saveResult.ToNewResult<ScheduleDetailDto>();
        }

        return GenericResult<ScheduleDetailDto>.Success(MapToDetail(config, request, scheduleId));
    }

    // What a create request leaves out takes the caller's tenant's setting (FDW-3580): whether the schedule runs, and how often and how long
    // apart it retries a failed run, which no request names. Mike: "make it configurable ... as long as it is in configuration and not hardcoded".
    private IGenericResult ApplyDefaults(TConfig config, CreateScheduleRequest request, Guid? tenantId)
    {
        var enabled = InstanceDefault.Resolve(
            request.IsEnabled, _settings, DefaultValueSettings.DefaultScheduleIsEnabled, tenantId, "schedule", request.Name, "IsEnabled", Logger);
        if (enabled.IsFailure)
        {
            return enabled;
        }

        var retries = InstanceDefault.Resolve<int>(
            null, _settings, DefaultValueSettings.DefaultScheduleMaxRetries, tenantId, "schedule", request.Name, "MaxRetries", Logger);
        if (retries.IsFailure)
        {
            return retries;
        }

        var delay = InstanceDefault.Resolve<int>(
            null, _settings, DefaultValueSettings.DefaultScheduleRetryDelaySeconds, tenantId, "schedule", request.Name, "RetryDelaySeconds", Logger);
        if (delay.IsFailure)
        {
            return delay;
        }

        config.IsEnabled = enabled.Value;
        config.MaxRetries = retries.Value;
        config.RetryDelaySeconds = delay.Value;
        return GenericResult.Success();
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
