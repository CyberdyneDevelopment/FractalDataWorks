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
/// Base endpoint for creating a tenant-level setting override.
/// </summary>
public abstract class CreateTenantSettingEndpointBase : CrudCreateEndpointBase<CreateTenantSettingRequest, TenantSettingSummaryDto>
{
    // Why: SettingsConfigurationProvider replaces IOptionsMonitor<List<TenantSettingConfiguration>>
    // with dual-source (ctrl + cfg) provider that provides server/tenant/role settings.
    private readonly SettingsConfigurationProvider _provider;

    /// <inheritdoc />
    protected CreateTenantSettingEndpointBase(SettingsConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    protected override string ResourceName => "settings/tenant";

    /// <inheritdoc />
    protected override string EndpointSummary => "Create a tenant setting override";

    /// <inheritdoc />
    protected override string GetResourceName(CreateTenantSettingRequest request) => request.SettingName;

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> CheckExists(CreateTenantSettingRequest request, CancellationToken ct)
    {
        var tenantSettingsResult = await _provider.GetTenantSettings(ct).ConfigureAwait(false);
        var tenantSettings = tenantSettingsResult.IsSuccess ? tenantSettingsResult.Value! : (IReadOnlyList<TenantSettingConfiguration>)[];
        var existing = tenantSettings
            .FirstOrDefault(s => s.TenantId == request.TenantId
                                 && string.Equals(s.SettingName, request.SettingName, StringComparison.OrdinalIgnoreCase));
        return GenericResult<bool>.Success(existing is not null);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<TenantSettingSummaryDto>> Create(CreateTenantSettingRequest request, CancellationToken ct)
    {
        var config = new TenantSettingConfiguration
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            SettingName = request.SettingName,
            SettingValue = request.SettingValue,
            IsActive = true
        };

        var saveResult = await _provider.SaveTenantSetting(config, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            return saveResult.ToNewResult<TenantSettingSummaryDto>();
        }

        SettingsEndpointLog.CreatedTenantSetting(Logger, request.SettingName, request.TenantId.ToString());

        var dto = new TenantSettingSummaryDto
        {
            Id = config.Id,
            TenantId = config.TenantId,
            SettingName = config.SettingName,
            SettingValue = config.SettingValue,
            IsActive = config.IsActive
        };

        return GenericResult<TenantSettingSummaryDto>.Success(dto);
    }

    /// <inheritdoc />
    protected override Task SendCreatedResponse(TenantSettingSummaryDto detail, CancellationToken ct)
    {
        return Send.ResponseAsync(detail, 201, ct);
    }
}
