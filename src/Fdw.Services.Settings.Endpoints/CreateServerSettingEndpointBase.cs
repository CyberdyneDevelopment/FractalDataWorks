using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Configuration;
using Fdw.Services.Settings.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Base endpoint for creating a new server-level setting.
/// </summary>
public abstract class CreateServerSettingEndpointBase : CrudCreateEndpointBase<CreateServerSettingRequest, ServerSettingDetailDto>
{
    private readonly IServerSettingConfigurationProvider _provider;

    /// <inheritdoc />
    protected CreateServerSettingEndpointBase(ILogger<CreateServerSettingEndpointBase> logger, IServerSettingConfigurationProvider provider) : base(logger)
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
        var existingResult = await _provider.Get(request.SettingName, ct).ConfigureAwait(false);
        if (!existingResult.IsSuccess)
            return existingResult.ToNewResult<bool>();
        return GenericResult<bool>.Success(existingResult.Value is not null);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<ServerSettingDetailDto>> Create(CreateServerSettingRequest request, CancellationToken ct)
    {
        var config = new ServerSettingImplementationConfiguration
        {
            Name = request.SettingName,
            SettingValue = request.SettingValue,
            DataType = request.DataType,
            Description = request.Description,
            MinValue = request.MinValue,
            MaxValue = request.MaxValue,
            IsActive = true
        };

        var saveResult = await _provider.Save(config, "ServerSetting", "ServerSetting", config.Name, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            return saveResult.ToNewResult<ServerSettingDetailDto>();
        }

        SettingsEndpointLog.CreatedServerSetting(Logger, request.SettingName);

        var detail = new ServerSettingDetailDto
        {
            Id = config.Id,
            SettingName = config.Name,
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
