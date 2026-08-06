using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Settings.Configuration;
using Fdw.Services.Settings.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Base endpoint for creating a role-level setting override.
/// </summary>
public abstract class CreateRoleSettingEndpointBase : CrudCreateEndpoint<CreateRoleSettingRequest, RoleSettingSummaryDto>
{
    // Why: SettingsConfigurationProvider replaces IOptionsMonitor<List<RoleSettingConfiguration>>
    // with dual-source (ctrl + cfg) provider that provides server/tenant/role settings.
    private readonly SettingsConfigurationProvider _provider;

    /// <inheritdoc />
    protected CreateRoleSettingEndpointBase(SettingsConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    protected override string ResourceName => "settings/role";

    /// <inheritdoc />
    protected override string EndpointSummary => "Create a role setting override";

    /// <inheritdoc />
    protected override string GetResourceName(CreateRoleSettingRequest request) => request.SettingName;

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> CheckExists(CreateRoleSettingRequest request, CancellationToken ct)
    {
        var roleSettingsResult = await _provider.GetRoleSettings(ct).ConfigureAwait(false);
        var roleSettings = roleSettingsResult.IsSuccess ? roleSettingsResult.Value! : (IReadOnlyList<RoleSettingConfiguration>)[];
        var existing = roleSettings
            .FirstOrDefault(s => s.TenantId == request.TenantId
                                 && string.Equals(s.RoleName, request.RoleName, StringComparison.OrdinalIgnoreCase)
                                 && string.Equals(s.SettingName, request.SettingName, StringComparison.OrdinalIgnoreCase));
        return GenericResult<bool>.Success(existing is not null);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<RoleSettingSummaryDto>> Create(CreateRoleSettingRequest request, CancellationToken ct)
    {
        var config = new RoleSettingConfiguration
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            RoleName = request.RoleName,
            SettingName = request.SettingName,
            SettingValue = request.SettingValue,
            IsActive = true
        };

        var saveResult = await _provider.SaveRoleSetting(config, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            return saveResult.ToNewResult<RoleSettingSummaryDto>();
        }

        SettingsEndpointLog.CreatedRoleSetting(Logger, request.SettingName, request.RoleName, request.TenantId.ToString());

        var dto = new RoleSettingSummaryDto
        {
            Id = config.Id,
            TenantId = config.TenantId,
            RoleName = config.RoleName,
            SettingName = config.SettingName,
            SettingValue = config.SettingValue,
            IsActive = config.IsActive
        };

        return GenericResult<RoleSettingSummaryDto>.Success(dto);
    }

    /// <inheritdoc />
    protected override Task SendCreatedResponse(RoleSettingSummaryDto detail, CancellationToken ct)
    {
        return Send.ResponseAsync(detail, 201, ct);
    }
}
