using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Configuration;
using Fdw.Services.Settings.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Base endpoint for updating an existing server-level setting.
/// </summary>
public abstract class UpdateServerSettingEndpointBase : CrudUpdateEndpoint<UpdateServerSettingRequest, ServerSettingDetailDto>
{
    // Why: SettingsConfigurationProvider replaces IOptionsMonitor<List<ServerSettingConfiguration>>
    // with dual-source (ctrl + cfg) provider that provides server/tenant/role settings.
    private readonly SettingsConfigurationProvider _provider;

    /// <inheritdoc />
    protected UpdateServerSettingEndpointBase(SettingsConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    protected override string ResourceName => "settings/server";

    /// <inheritdoc />
    protected override string Route => "/settings/server/{SettingName}";

    /// <inheritdoc />
    protected override string EndpointSummary => "Update a server setting";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(UpdateServerSettingRequest request) => request.SettingName;

    /// <inheritdoc />
    protected override async Task<IGenericResult<ServerSettingDetailDto?>> FindForUpdate(UpdateServerSettingRequest request, CancellationToken ct)
    {
        var settingResult = await _provider.GetServerSetting(request.SettingName, ct).ConfigureAwait(false);
        var setting = settingResult.IsSuccess ? settingResult.Value : null;

        if (setting is null)
        {
            // Why: the Update path only mutates existing rows, but a recognized platform
            // setting (e.g. Enable2FA, SystemName) has no row until first write. For names in
            // the SettingDefinitions allow-list, return a synthesized detail so the update
            // proceeds and materializes the row. Unknown names still 404 (no fallback).
            if (SettingDefinitions.TryGet(request.SettingName, out var definition))
            {
                return GenericResult<ServerSettingDetailDto?>.Success(new ServerSettingDetailDto
                {
                    Id = Guid.Empty,
                    SettingName = request.SettingName,
                    SettingValue = string.Empty,
                    DataType = definition.DataType,
                    Description = definition.Description,
                    IsActive = true
                });
            }

            SettingsEndpointLog.ServerSettingNotFound(Logger, request.SettingName);
            return GenericResult<ServerSettingDetailDto?>.Success(null);
        }

        var detail = new ServerSettingDetailDto
        {
            Id = setting.Id,
            SettingName = setting.SettingName,
            SettingValue = setting.SettingValue,
            DataType = setting.DataType,
            Description = setting.Description,
            MinValue = setting.MinValue,
            MaxValue = setting.MaxValue,
            IsActive = setting.IsActive
        };

        return GenericResult<ServerSettingDetailDto?>.Success(detail);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<ServerSettingDetailDto>> Update(
        UpdateServerSettingRequest request,
        ServerSettingDetailDto existing,
        CancellationToken ct)
    {
        var settingResult = await _provider.GetServerSetting(request.SettingName, ct).ConfigureAwait(false);
        var setting = settingResult.IsSuccess ? settingResult.Value : null;

        if (setting is null)
        {
            // Why: known platform setting with no row yet — materialize it from the
            // registry definition (FindForUpdate already gated this to allow-listed names).
            if (!SettingDefinitions.TryGet(request.SettingName, out var definition))
            {
                SettingsEndpointLog.ServerSettingNotFound(Logger, request.SettingName);
                return GenericResult<ServerSettingDetailDto>.Failure(
                    SettingsEndpointLog.ServerSettingNotFound(Logger, request.SettingName));
            }

            setting = new ServerSettingConfiguration
            {
                SettingName = request.SettingName,
                DataType = definition.DataType,
                Description = definition.Description,
                IsActive = true
            };
        }

        if (request.SettingValue is not null) setting.SettingValue = request.SettingValue;
        if (request.Description is not null) setting.Description = request.Description;
        if (request.MinValue is not null) setting.MinValue = request.MinValue;
        if (request.MaxValue is not null) setting.MaxValue = request.MaxValue;
        if (request.IsActive.HasValue) setting.IsActive = request.IsActive.Value;

        var saveResult = await _provider.SaveServerSetting(setting, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            return saveResult.ToNewResult<ServerSettingDetailDto>();
        }

        SettingsEndpointLog.UpdatedServerSetting(Logger, request.SettingName);

        var detail = new ServerSettingDetailDto
        {
            Id = setting.Id,
            SettingName = setting.SettingName,
            SettingValue = setting.SettingValue,
            DataType = setting.DataType,
            Description = setting.Description,
            MinValue = setting.MinValue,
            MaxValue = setting.MaxValue,
            IsActive = setting.IsActive
        };

        return GenericResult<ServerSettingDetailDto>.Success(detail);
    }
}
