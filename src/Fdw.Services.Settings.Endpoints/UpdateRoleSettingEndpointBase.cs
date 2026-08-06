using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Settings.Configuration;
using Fdw.Services.Settings.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Base endpoint for updating a role-level setting override.
/// </summary>
public abstract class UpdateRoleSettingEndpointBase : CrudUpdateEndpoint<UpdateRoleSettingRequest, RoleSettingSummaryDto>
{
    // Why: SettingsConfigurationProvider replaces IOptionsMonitor<List<RoleSettingConfiguration>>
    // with dual-source (ctrl + cfg) provider that provides server/tenant/role settings.
    private readonly SettingsConfigurationProvider _provider;

    /// <inheritdoc />
    protected UpdateRoleSettingEndpointBase(SettingsConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    protected override string ResourceName => "settings/role";

    /// <inheritdoc />
    protected override string Route => "/settings/role/{TenantId}/{RoleName}/{SettingName}";

    /// <inheritdoc />
    protected override string EndpointSummary => "Update a role setting override";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(UpdateRoleSettingRequest request)
        => $"{request.TenantId}/{request.RoleName}/{request.SettingName}";

    /// <inheritdoc />
    protected override async Task<IGenericResult<RoleSettingSummaryDto?>> FindForUpdate(UpdateRoleSettingRequest request, CancellationToken ct)
    {
        var setting = await FindRoleSetting(request.TenantId, request.RoleName, request.SettingName, ct).ConfigureAwait(false);

        if (setting is null)
        {
            SettingsEndpointLog.RoleSettingNotFound(Logger, request.SettingName, request.RoleName, request.TenantId.ToString());
            return GenericResult<RoleSettingSummaryDto?>.Success(null);
        }

        var dto = new RoleSettingSummaryDto
        {
            Id = setting.Id,
            TenantId = setting.TenantId,
            RoleName = setting.RoleName,
            SettingName = setting.SettingName,
            SettingValue = setting.SettingValue,
            IsActive = setting.IsActive
        };

        return GenericResult<RoleSettingSummaryDto?>.Success(dto);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<RoleSettingSummaryDto>> Update(
        UpdateRoleSettingRequest request,
        RoleSettingSummaryDto existing,
        CancellationToken ct)
    {
        var setting = await FindRoleSetting(request.TenantId, request.RoleName, request.SettingName, ct).ConfigureAwait(false);

        if (setting is null)
        {
            return GenericResult<RoleSettingSummaryDto>.Failure(
                SettingsEndpointLog.RoleSettingNotFound(Logger, request.SettingName, request.RoleName, request.TenantId.ToString()));
        }

        if (request.SettingValue is not null) setting.SettingValue = request.SettingValue;
        if (request.IsActive.HasValue) setting.IsActive = request.IsActive.Value;

        var saveResult = await _provider.SaveRoleSetting(setting, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            return saveResult.ToNewResult<RoleSettingSummaryDto>();
        }

        SettingsEndpointLog.UpdatedRoleSetting(Logger, request.SettingName, request.RoleName, request.TenantId.ToString());

        var dto = new RoleSettingSummaryDto
        {
            Id = setting.Id,
            TenantId = setting.TenantId,
            RoleName = setting.RoleName,
            SettingName = setting.SettingName,
            SettingValue = setting.SettingValue,
            IsActive = setting.IsActive
        };

        return GenericResult<RoleSettingSummaryDto>.Success(dto);
    }

    // Why: Compound-key lookup for role settings (TenantId + RoleName + SettingName).
    // The provider doesn't have a compound-key Get method, so we load all and filter.
    private async Task<RoleSettingConfiguration?> FindRoleSetting(
        Guid tenantId, string roleName, string settingName, CancellationToken ct)
    {
        var roleSettingsResult = await _provider.GetRoleSettings(ct).ConfigureAwait(false);
        var roleSettings = roleSettingsResult.IsSuccess ? roleSettingsResult.Value! : (IReadOnlyList<RoleSettingConfiguration>)[];
        return roleSettings
            .FirstOrDefault(s => s.TenantId == tenantId
                                 && string.Equals(s.RoleName, roleName, StringComparison.OrdinalIgnoreCase)
                                 && string.Equals(s.SettingName, settingName, StringComparison.OrdinalIgnoreCase));
    }
}
