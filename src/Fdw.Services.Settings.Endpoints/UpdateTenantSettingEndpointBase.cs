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
/// Base endpoint for updating a tenant-level setting override.
/// </summary>
public abstract class UpdateTenantSettingEndpointBase : CrudUpdateEndpointBase<UpdateTenantSettingRequest, TenantSettingSummaryDto>
{
    // Why: SettingsConfigurationProvider replaces IOptionsMonitor<List<TenantSettingConfiguration>>
    // with dual-source (ctrl + cfg) provider that provides server/tenant/role settings.
    private readonly SettingsConfigurationProvider _provider;

    /// <inheritdoc />
    protected UpdateTenantSettingEndpointBase(SettingsConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    protected override string ResourceName => "settings/tenant";

    /// <inheritdoc />
    protected override string Route => "/settings/tenant/{TenantId}/{SettingName}";

    /// <inheritdoc />
    protected override string EndpointSummary => "Update a tenant setting override";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(UpdateTenantSettingRequest request)
        => $"{request.TenantId}/{request.SettingName}";

    /// <inheritdoc />
    protected override async Task<IGenericResult<TenantSettingSummaryDto?>> FindForUpdate(UpdateTenantSettingRequest request, CancellationToken ct)
    {
        var setting = await FindTenantSetting(request.TenantId, request.SettingName, ct).ConfigureAwait(false);

        if (setting is null)
        {
            SettingsEndpointLog.TenantSettingNotFound(Logger, request.SettingName, request.TenantId.ToString());
            return GenericResult<TenantSettingSummaryDto?>.Success(null);
        }

        var dto = new TenantSettingSummaryDto
        {
            Id = setting.Id,
            TenantId = setting.TenantId,
            SettingName = setting.SettingName,
            SettingValue = setting.SettingValue,
            IsActive = setting.IsActive
        };

        return GenericResult<TenantSettingSummaryDto?>.Success(dto);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<TenantSettingSummaryDto>> Update(
        UpdateTenantSettingRequest request,
        TenantSettingSummaryDto existing,
        CancellationToken ct)
    {
        var setting = await FindTenantSetting(request.TenantId, request.SettingName, ct).ConfigureAwait(false);

        if (setting is null)
        {
            return GenericResult<TenantSettingSummaryDto>.Failure(
                SettingsEndpointLog.TenantSettingNotFound(Logger, request.SettingName, request.TenantId.ToString()));
        }

        if (request.SettingValue is not null) setting.SettingValue = request.SettingValue;
        if (request.IsActive.HasValue) setting.IsActive = request.IsActive.Value;

        var saveResult = await _provider.SaveTenantSetting(setting, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            return saveResult.ToNewResult<TenantSettingSummaryDto>();
        }

        SettingsEndpointLog.UpdatedTenantSetting(Logger, request.SettingName, request.TenantId.ToString());

        var dto = new TenantSettingSummaryDto
        {
            Id = setting.Id,
            TenantId = setting.TenantId,
            SettingName = setting.SettingName,
            SettingValue = setting.SettingValue,
            IsActive = setting.IsActive
        };

        return GenericResult<TenantSettingSummaryDto>.Success(dto);
    }

    // Why: Compound-key lookup for tenant settings (TenantId + SettingName).
    // The provider doesn't have a compound-key Get method, so we load all and filter.
    private async Task<TenantSettingConfiguration?> FindTenantSetting(
        Guid tenantId, string settingName, CancellationToken ct)
    {
        var tenantSettingsResult = await _provider.GetTenantSettings(ct).ConfigureAwait(false);
        var tenantSettings = tenantSettingsResult.IsSuccess ? tenantSettingsResult.Value! : (IReadOnlyList<TenantSettingConfiguration>)[];
        return tenantSettings
            .FirstOrDefault(s => s.TenantId == tenantId
                                 && string.Equals(s.SettingName, settingName, StringComparison.OrdinalIgnoreCase));
    }
}
