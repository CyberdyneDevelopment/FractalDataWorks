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
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Base endpoint for creating a tenant-level setting override.
/// </summary>
public abstract class CreateTenantSettingEndpointBase : CrudCreateEndpointBase<CreateTenantSettingRequest, TenantSettingSummaryDto>
{
    private readonly ITenantSettingConfigurationProvider _provider;

    /// <inheritdoc />
    protected CreateTenantSettingEndpointBase(ILogger<CreateTenantSettingEndpointBase> logger, ITenantSettingConfigurationProvider provider) : base(logger)
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
        var tenantSettingsResult = await _provider.Get(ct).ConfigureAwait(false);
        var tenantSettings = tenantSettingsResult.IsSuccess ? tenantSettingsResult.Value! : (IReadOnlyList<TenantSettingImplementationConfiguration>)[];
        var existing = tenantSettings
            .FirstOrDefault(s => s.TenantId == request.TenantId
                                 && string.Equals(s.SettingName, request.SettingName, StringComparison.OrdinalIgnoreCase));
        return GenericResult<bool>.Success(existing is not null);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<TenantSettingSummaryDto>> Create(CreateTenantSettingRequest request, CancellationToken ct)
    {
        var config = new TenantSettingImplementationConfiguration
        {
            // Why: Save()'s name param is the domain-row lookup key -- an existing row carrying
            // it is the row a save hangs from, and only its absence mints a new one. Left unset
            // (defaults to ""), every create/update resolved the SAME single domain row regardless
            // of tenant or setting name, so a second create silently overwrote the first instead
            // of adding an independent override. TenantId+SettingName together identify a member
            // here (unlike ServerSetting, where SettingName alone is unique).
            Name = $"{request.TenantId}/{request.SettingName}",
            TenantId = request.TenantId,
            SettingName = request.SettingName,
            SettingValue = request.SettingValue,
            IsActive = true
        };

        var saveResult = await _provider.Save(config, "TenantSetting", "TenantSetting", config.Name, ct).ConfigureAwait(false);
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
