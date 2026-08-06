using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Settings.Configuration;
using Fdw.Services.Settings.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Base endpoint for creating a new server-level setting.
/// </summary>
public abstract class CreateServerSettingEndpointBase : CrudCreateEndpoint<CreateServerSettingRequest, ServerSettingDetailDto>
{
    // Why: SettingsConfigurationProvider replaces IOptionsMonitor<List<ServerSettingConfiguration>>
    // with dual-source (ctrl + cfg) provider that provides server/tenant/role settings.
    private readonly SettingsConfigurationProvider _provider;

    /// <inheritdoc />
    protected CreateServerSettingEndpointBase(SettingsConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    protected override string ResourceName => "settings/server";

    /// <inheritdoc />
    protected override string EndpointSummary => "Create a new server setting";

    /// <inheritdoc />
    protected override string GetResourceName(CreateServerSettingRequest request) => request.SettingName;

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> CheckExists(CreateServerSettingRequest request, CancellationToken ct)
    {
        var existingResult = await _provider.GetServerSetting(request.SettingName, ct).ConfigureAwait(false);
        return GenericResult<bool>.Success(existingResult.IsSuccess && existingResult.Value is not null);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<ServerSettingDetailDto>> Create(CreateServerSettingRequest request, CancellationToken ct)
    {
        var config = new ServerSettingConfiguration
        {
            Id = Guid.NewGuid(),
            SettingName = request.SettingName,
            SettingValue = request.SettingValue,
            DataType = request.DataType,
            Description = request.Description,
            MinValue = request.MinValue,
            MaxValue = request.MaxValue,
            IsActive = true
        };

        var saveResult = await _provider.SaveServerSetting(config, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            return saveResult.ToNewResult<ServerSettingDetailDto>();
        }

        SettingsEndpointLog.CreatedServerSetting(Logger, request.SettingName);

        var detail = new ServerSettingDetailDto
        {
            Id = config.Id,
            SettingName = config.SettingName,
            SettingValue = config.SettingValue,
            DataType = config.DataType,
            Description = config.Description,
            MinValue = config.MinValue,
            MaxValue = config.MaxValue,
            IsActive = config.IsActive
        };

        return GenericResult<ServerSettingDetailDto>.Success(detail);
    }

    /// <inheritdoc />
    protected override void OnAlreadyExists(string resourceName)
    {
        SettingsEndpointLog.ServerSettingAlreadyExists(Logger, resourceName);
    }

    /// <inheritdoc />
    protected override Task SendCreatedResponse(ServerSettingDetailDto detail, CancellationToken ct)
    {
        return Send.ResponseAsync(detail, 201, ct);
    }
}
